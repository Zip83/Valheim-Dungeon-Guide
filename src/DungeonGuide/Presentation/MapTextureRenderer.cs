using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Publishes the selected map while retaining every prepared floor until the dungeon snapshot is replaced.</summary>
internal sealed class MapTextureRenderer : IDisposable
{
    private const int Size = 768;
    private const double PrefetchShare = 0.35;
    private readonly FloorResourceCache<CachedFloorSurface> _floors = new();
    private readonly List<int> _wanted = new();
    private readonly Func<int, bool> _isReady;
    private readonly FloorPrefetchScheduler _prefetch = new();
    private readonly MapMarkerRenderer _markers = new();
    private readonly FrameWorkBudget _budget = new(8192, 1);
    private readonly MapExtentFitter _extentFitter;
    private CachedFloorSurface _published;
    private DungeonSession _session;
    private bool _reveal;
    private bool _hasPublishedImage;
    private float _firstStarted;
    private int _physicalFloor = -1;
    private int _selectedFloor = -1;
    private int _priorityCount;
    private NavGraph _requestedGraph;
    private float _requestedAt;
    private bool _requestedWarm;
    private int _requestedFloor = -1;

    /// <summary>Uses a shared floor-fitting policy for the map and overlay.</summary>
    public MapTextureRenderer(MapExtentFitter extentFitter)
    {
        _extentFitter = extentFitter;
        _isReady = IsFloorReady;
    }

    /// <summary>Last fully initialized surface, or null before first publication.</summary>
    public Texture2D Texture => _published?.Texture;

    /// <summary>Transparent marker layer, independent of fog compositing.</summary>
    public Texture2D Markers => _markers.Texture;

    /// <summary>Projection paired with the actually displayed surface.</summary>
    public FloorPlanRaster Published => _published?.Raster;

    /// <summary>Displayed physical floor, or -1 while the first map is being prepared.</summary>
    public int DisplayedFloor => _published?.Floor ?? -1;

    /// <summary>Physical elevation paired with the published image.</summary>
    public float DisplayedHeight => _published?.Height ?? float.NaN;

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

    /// <summary>Publishes cached floors immediately and reserves bounded neighbor progress before ongoing foreground updates.</summary>
    public void Tick(DungeonSession session, MapViewState view)
    {
        EnsureSession(session);
        if (session?.Scan == null)
        {
            return;
        }

        SetMode(view.RevealAll);
        _budget.BeginFrame();
        TrackFloorRequest(session, view.SelectedFloor);
        UpdatePreloadOrder(session, view);
        AdvanceFloors(session);
        _floors.Retain(session.Scan.Graph, _wanted, _published);
    }

    private void EnsureSession(DungeonSession session)
    {
        if (session != null && _session != session)
        {
            Reset();
            _session = session;
            _firstStarted = Time.realtimeSinceStartup;
        }
    }

    private void SetMode(bool reveal)
    {
        if (_reveal == reveal)
        {
            return;
        }

        foreach (var floor in _floors.Resources)
        {
            floor.SetMode(reveal);
        }

        _reveal = reveal;
        _markers.Clear();
    }

    private void TrackFloorRequest(DungeonSession session, int floor)
    {
        if (_requestedGraph != session.Scan.Graph || _requestedFloor != floor)
        {
            _requestedGraph = session.Scan.Graph;
            _requestedFloor = floor;
            _requestedAt = Time.realtimeSinceStartup;
            _requestedWarm = IsFloorReady(floor);
        }
    }

    private void UpdatePreloadOrder(DungeonSession session, MapViewState view)
    {
        if (_physicalFloor != view.CurrentFloor || _selectedFloor != view.SelectedFloor
            || _wanted.Count != session.Floors.Count)
        {
            _priorityCount = FloorPreloadPolicy.Fill(_wanted, view.CurrentFloor, view.SelectedFloor, session.Floors.Count);
            _physicalFloor = view.CurrentFloor;
            _selectedFloor = view.SelectedFloor;
            _prefetch.Reset();
        }
    }

    private CachedFloorSurface GetFloor(DungeonSession session, int floor)
    {
        if (!_floors.TryGet(session.Scan.Graph, floor, out var surface))
        {
            surface = new CachedFloorSurface(session, floor, Size);
            _floors.Add(session.Scan.Graph, floor, surface);
        }

        return surface;
    }

    private void AdvanceFloors(DungeonSession session)
    {
        if (_wanted.Count == 0)
        {
            return;
        }

        var selected = GetFloor(session, _wanted[0]);
        PublishIfReady(selected, session.NavigationReady);
        if (selected.Ready)
        {
            AdvanceBackground(session, selected, _budget.CreateSlice(PrefetchShare));
        }

        selected.Tick(session, _reveal, _budget);
        PublishIfReady(selected, session.NavigationReady);
        AdvanceBackground(session, selected, _budget);
    }

    private void PublishIfReady(CachedFloorSurface selected, bool navigationReady)
    {
        if (!selected.Ready || ReferenceEquals(selected, _published))
        {
            return;
        }

        _published = selected;
        LogFloorPublication(selected);
        if (!_hasPublishedImage)
        {
            _hasPublishedImage = true;
            LogFirstImage(navigationReady);
        }
    }

    private void AdvanceBackground(DungeonSession session, CachedFloorSurface selected, FrameWorkBudget budget)
    {
        if (_prefetch.TrySelect(_wanted, _priorityCount, selected.Ready, _isReady, budget, out var floor))
        {
            GetFloor(session, floor).Tick(session, _reveal, budget);
        }
    }

    private bool IsFloorReady(int floor) =>
        _floors.TryGet(_session.Scan.Graph, floor, out var surface) && surface.Ready;

    private void LogFirstImage(bool navigationReady)
    {
        Debug.Log("DungeonGuide first floor image: elapsedSeconds="
            + (Time.realtimeSinceStartup - _firstStarted).ToString("F3", System.Globalization.CultureInfo.InvariantCulture)
            + ", navigationReady=" + navigationReady);
    }

    /// <summary>Records cache reuse and elapsed wait only when another completed image becomes visible.</summary>
    private void LogFloorPublication(CachedFloorSurface selected)
    {
        Debug.Log("DungeonGuide floor image published: floor=" + selected.Floor + ", preloaded=" + _requestedWarm
            + ", waitMilliseconds=" + ((Time.realtimeSinceStartup - _requestedAt) * 1000)
                .ToString("F1", System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>Uses current displayed fog for pointer selection.</summary>
    public bool IsVisible(Point position) => Published != null && (_reveal || _published.Fog.Visible(position));

    /// <summary>Refreshes dynamic markers independently of static terrain.</summary>
    public void Render(DungeonSession session, MapViewState view, NavigationCoordinator navigator, Player player)
    {
        if (Published != null && _session == session && Published.Graph == session.Scan.Graph)
        {
            _markers.Render(session, view, navigator, player, Published, _published.Fog, _reveal);
        }
    }

    /// <summary>Releases retained floor textures on actual world changes while ordinary dungeon exits keep them warm.</summary>
    public void ClearWorld() => Reset();

    private void Reset()
    {
        _floors.Dispose();
        _published = null;
        _session = null;
        _wanted.Clear();
        _physicalFloor = _selectedFloor = -1;
        _requestedGraph = null;
        _requestedFloor = -1;
        _requestedWarm = false;
        _priorityCount = 0;
        _prefetch.Reset();
        _hasPublishedImage = false;
        _reveal = false;
        _markers.Clear();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Reset();
        _markers.Dispose();
    }
}
