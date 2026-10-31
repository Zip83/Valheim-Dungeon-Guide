using System;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Owns published and pending floor surfaces independently of dynamic markers.</summary>
internal sealed class MapTextureRenderer : IDisposable
{
    private const int Size = 768;
    private readonly FloorPlanCache _cache = new();
    private readonly MapMarkerRenderer _markers = new();
    private readonly FrameWorkBudget _budget = new(8192, 1);
    private readonly MapExtentFitter _extentFitter;
    private readonly Exploration _noVisits = new();
    private FloorSurfaceRenderer _surface;
    private FloorSurfaceRenderer _pending;
    private FloorPlanRaster _pendingRaster;
    private DungeonSession _session;
    private bool _reveal;
    private float _firstStarted;
    private float _pendingHeight;

    /// <summary>Uses a shared floor-fitting policy for the map and overlay.</summary>
    public MapTextureRenderer(MapExtentFitter extentFitter)
    {
        _extentFitter = extentFitter;
    }

    /// <summary>Last fully initialized surface, or null before first publication.</summary>
    public Texture2D Texture => _surface?.Texture;

    /// <summary>Private GPU material, or null when CPU compositing is selected.</summary>
    public Material Material => _surface?.Material;

    /// <summary>Transparent marker layer, independent of fog compositing.</summary>
    public Texture2D Markers => _markers.Texture;

    /// <summary>Projection paired with the actually displayed surface.</summary>
    public FloorPlanRaster Published
    {
        get; private set;
    }

    /// <summary>Displayed physical floor, or -1 while the first map is being prepared.</summary>
    public int DisplayedFloor => Published?.Floor ?? -1;

    /// <summary>Physical elevation paired with the published image, even while its replacement is pending.</summary>
    public float DisplayedHeight { get; private set; } = float.NaN;

    /// <summary>Width of the displayed map in world metres.</summary>
    public float Span => Published?.Span ?? 1;

    /// <summary>Projects a world position into the published surface.</summary>
    public Vector2 UV(Point p) => Published == null ? new Vector2(0.5f, 0.5f)
        : new Vector2(Published.X(p.x) / Size, Published.Y(p.z) / Size);

    /// <summary>Fits a selected floor using its conservative sampled extent.</summary>
    public Rect FitFloor(DungeonSession session, int floor)
    {
        var fit = _extentFitter.Fit(session.Scan.Graph, floor, UnityPoints.P(session.Area.Bounds.center),
            Mathf.Max(1, Mathf.Max(session.Area.Bounds.size.x, session.Area.Bounds.size.z) + 3));
        return new Rect(fit.X, fit.Y, fit.Size, fit.Size);
    }

    /// <summary>Advances raster construction and dirty fog tiles under a shared presentation budget.</summary>
    public void Tick(DungeonSession session, MapViewState view)
    {
        if (_session != session)
        {
            Reset();
            _session = session;
            _firstStarted = Time.realtimeSinceStartup;
        }

        if (session?.Scan == null)
        {
            return;
        }

        if (_surface != null && _reveal != view.RevealAll)
        {
            // A full-map texture must never remain visible while explored-mode tiles catch up.
            _surface?.Dispose();
            _surface = null;
            Published = null;
            _pending?.Dispose();
            _pending = null;
            _pendingRaster = null;
        }

        _budget.BeginFrame();
        var span = Mathf.Max(1, Mathf.Max(session.Area.Bounds.size.x, session.Area.Bounds.size.z) + 3);
        _cache.Tick(session.Scan.Graph, view.SelectedFloor, session.Scan.Spacing, Size,
            UnityPoints.P(session.Area.Bounds.center), span, _budget);
        var raster = _cache.Ready;
        if (raster == null)
        {
            return;
        }

        if (raster != Published)
        {
            if (_pendingRaster != raster)
            {
                _pending?.Dispose();
                _pending = new FloorSurfaceRenderer(raster);
                _pendingRaster = raster;
                _pendingHeight = raster.Graph == session.Scan.Graph && raster.Floor < session.Floors.Count
                    ? session.Floors[raster.Floor] : DisplayedHeight;
            }

            _pending.Tick(session.Exploration, _noVisits, view.RevealAll, _budget);
            if (_pending.Ready)
            {
                var first = Published == null;
                _surface?.Dispose();
                _surface = _pending;
                _pending = null;
                _pendingRaster = null;
                Published = raster;
                DisplayedHeight = _pendingHeight;
                if (first)
                {
                    Debug.Log("DungeonGuide first floor image: elapsedSeconds="
                        + (Time.realtimeSinceStartup - _firstStarted).ToString("F3", System.Globalization.CultureInfo.InvariantCulture)
                        + ", navigationReady=" + session.NavigationReady);
                }
            }
        }
        else
        {
            _surface.Tick(session.Exploration, _noVisits, view.RevealAll, _budget);
        }

        _reveal = view.RevealAll;
    }

    /// <summary>Uses current displayed fog for pointer selection.</summary>
    public bool IsVisible(Point position) => Published != null && (_reveal || _surface.Fog.Visible(position));

    /// <summary>Refreshes dynamic markers independently of static terrain.</summary>
    public void Render(DungeonSession session, MapViewState view, NavigationCoordinator navigator, Player player)
    {
        if (Published != null && _session == session && Published.Graph == session.Scan.Graph)
        {
            _markers.Render(session, view, navigator, player, Published, _surface.Fog, _reveal);
        }
    }

    private void Reset()
    {
        _surface?.Dispose();
        _pending?.Dispose();
        _surface = _pending = null;
        Published = _pendingRaster = null;
        _cache.Dispose();
        _markers.Clear();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Reset();
        _markers.Dispose();
    }
}
