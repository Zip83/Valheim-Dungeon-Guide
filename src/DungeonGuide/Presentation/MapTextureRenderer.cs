using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Schedules cached floor preparation independently of dynamic route and player markers.
/// </summary>
internal sealed class MapTextureRenderer : IDisposable
{
    private const int Size = 768;
    private readonly FloorPlanCache _cache = new();
    private readonly FloorFogCache _fog = new();
    private readonly MapMarkerRenderer _markers = new();
    private readonly FrameWorkBudget _budget = new(8192, 1);
    private readonly Color32[] _pixels = new Color32[Size * Size];
    private readonly MapExtentFitter _extentFitter;
    private IEnumerator<byte> _paint;
    private FloorPlanRaster _painting;
    private FloorFogMask _paintingFog;
    private FloorFogMask _publishedFog;
    private bool _publishedReveal;
    private readonly Color32[] _cloud = new Color32[Size * Size];
    private bool _cloudReady;
    private bool _reveal;
    private DungeonSession _session;

    /// <summary>Creates the shared terrain texture and its floor-extent policy.</summary>
    public MapTextureRenderer(MapExtentFitter extentFitter)
    {
        _extentFitter = extentFitter;
        Texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    /// <summary>Last complete floor image, unchanged while a replacement is prepared.</summary>
    public Texture2D Texture
    {
        get;
    }

    /// <summary>Independent transparent route and player layer.</summary>
    public Texture2D Markers => _markers.Texture;

    /// <summary>Grid corresponding to the published terrain and marker projection.</summary>
    public FloorPlanRaster Published
    {
        get; private set;
    }

    /// <summary>Floor of the image actually being displayed, or -1 before the first publication.</summary>
    public int DisplayedFloor => Published?.Floor ?? -1;

    /// <summary>World-space width of the published map.</summary>
    public float Span => Published?.Span ?? 1;

    /// <summary>Projects a position using the same coordinates as the published terrain.</summary>
    public Vector2 UV(Point p) => Published == null ? new Vector2(0.5f, 0.5f)
        : new Vector2(Published.X(p.x) / Size, Published.Y(p.z) / Size);

    /// <summary>Fits the displayed floor rather than a replacement that has not finished rendering.</summary>
    public Rect FitFloor(DungeonSession session, int floor)
    {
        var fit = _extentFitter.Fit(session.Scan.Graph, floor, UnityPoints.P(session.Area.Bounds.center),
            Mathf.Max(1, Mathf.Max(session.Area.Bounds.size.x, session.Area.Bounds.size.z) + 3));
        return new Rect(fit.X, fit.Y, fit.Size, fit.Size);
    }

    /// <summary>Advances geometry and terrain rows under one shared per-frame soft budget.</summary>
    public void Tick(DungeonSession session, MapViewState view)
    {
        if (_session != session)
        {
            Reset();
            _session = session;
        }

        if (session?.Scan == null)
        {
            return;
        }

        PrepareRaster(session, view);

        var ready = _cache.Ready;
        if (ready == null)
        {
            return;
        }

        _fog.Tick(ready, session.Exploration, _budget);
        var fog = _fog.Ready;
        if (fog == null || fog.Raster != ready)
        {
            return;
        }

        PreparePaint(ready, fog, view.RevealAll);

        AdvancePaint();
    }

    private IEnumerable<byte> Paint(FloorPlanRaster raster, FloorFogMask fog, bool reveal)
    {
        foreach (var step in PrepareCloud())
        {
            yield return step;
        }

        foreach (var step in PaintRows(raster, fog, reveal))
        {
            yield return step;
        }
    }

    /// <summary>Matches marker selection to the actually published fog rather than an in-flight mask.</summary>
    public bool IsVisible(Point position) => Published != null && (_publishedReveal || _publishedFog.Visible(position));

    /// <summary>Updates dynamic markers without rebuilding the floor geometry.</summary>
    public void Render(DungeonSession session, MapViewState view, NavigationCoordinator navigator, Player player)
    {
        if (Published != null && _session == session && Published.Graph == session.Scan.Graph)
        {
            _markers.Render(session, view, navigator, player, Published, _publishedFog, _publishedReveal);
        }
    }

    private void Reset()
    {
        _session = null;
        _paint?.Dispose();
        _paint = null;
        _painting = Published = null;
        _cache.Dispose();
        _fog.Dispose();
        _paintingFog = _publishedFog = null;
        _publishedReveal = false;
        _markers.Clear();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Reset();
        _markers.Dispose();
        UnityEngine.Object.Destroy(Texture);
    }

    private void AdvancePaint()
    {
        while (_paint != null && _budget.TryStartWork())
        {
            if (!_paint.MoveNext())
            {
                Texture.SetPixels32(_pixels);
                Texture.Apply(false);
                Published = _painting;
                _publishedFog = _paintingFog;
                _publishedReveal = _reveal;
                _paint.Dispose();
                _paint = null;
            }
        }
    }

    private void PrepareRaster(DungeonSession session, MapViewState view)
    {
        var span = Mathf.Max(1, Mathf.Max(session.Area.Bounds.size.x, session.Area.Bounds.size.z) + 3);
        _budget.BeginFrame();
        _cache.Tick(session.Scan.Graph, view.SelectedFloor, session.Scan.Spacing, Size,
            UnityPoints.P(session.Area.Bounds.center), span, _budget);
    }

    private void PreparePaint(FloorPlanRaster ready, FloorFogMask fog, bool reveal)
    {
        if (_painting != ready || _reveal != reveal || _paint == null && _paintingFog != fog)
        {
            _paint?.Dispose();
            _painting = ready;
            _paintingFog = fog;
            _reveal = reveal;
            _paint = Paint(ready, fog, _reveal).GetEnumerator();
        }
    }

    private IEnumerable<byte> PrepareCloud()
    {
        if (!_cloudReady)
        {
            for (var y = 0; y < Size; y++)
            {
                yield return 0;
                for (var x = 0; x < Size; x++)
                {
                    _cloud[y * Size + x] = MapPalette.Cloud(x, y);
                }
            }

            _cloudReady = true;
        }
    }

    private IEnumerable<byte> PaintRows(FloorPlanRaster raster, FloorFogMask fog, bool reveal)
    {
        for (var y = 0; y < Size; y++)
        {
            yield return 0;
            for (var x = 0; x < Size; x++)
            {
                var index = y * Size + x;
                var floor = raster.Owners[index] == 0 ? MapPalette.TextureBackground
                    : raster.Boundary[index] ? MapPalette.FloorOutline : MapPalette.ExploredFloor;
                _pixels[index] = reveal ? floor : Color32.Lerp(floor, _cloud[index], fog.Opacity[index] / 255f);
            }
        }
    }
}
