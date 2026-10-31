using System;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Owns one snapshot floor's cooperative raster build, masks and renderer resources.</summary>
internal sealed class CachedFloorSurface : IDisposable
{
    private readonly FloorPlanCache _plan = new();
    private readonly int _size;
    private readonly float _spacing;
    private readonly Point _center;
    private readonly float _span;
    private FloorSurfaceRenderer _surface;

    /// <summary>Captures immutable geometry, projection and physical height on the game thread.</summary>
    public CachedFloorSurface(DungeonSession session, int floor, int size)
    {
        Graph = session.Scan.Graph;
        _spacing = session.Scan.Spacing;
        Floor = floor;
        Height = session.Floors[floor];
        _size = size;
        _center = UnityPoints.P(session.Area.Bounds.center);
        _span = Mathf.Max(1, Mathf.Max(session.Area.Bounds.size.x, session.Area.Bounds.size.z) + 3);
    }

    /// <summary>Published geometry snapshot whose nodes belong to this entry.</summary>
    public NavGraph Graph
    {
        get;
    }

    /// <summary>Architectural index scoped to the source snapshot.</summary>
    public int Floor
    {
        get;
    }

    /// <summary>Physical height preserved when switching between cached floors.</summary>
    public float Height
    {
        get;
    }

    /// <summary>Whether the initial floor image and its opaque unknown mask are fully published.</summary>
    public bool Ready => _surface?.Ready == true;

    /// <summary>Completed projection; consumers publish it only when Ready is true.</summary>
    public FloorPlanRaster Raster => _plan.Ready;

    /// <summary>Owned terrain or CPU-composited image.</summary>
    public Texture2D Texture => _surface?.Texture;

    /// <summary>Fog paired with the currently uploaded texture.</summary>
    public FloorFogMask Fog => _surface?.Fog;

    /// <summary>Advances only this floor within the caller's existing shared presentation budget.</summary>
    public void Tick(DungeonSession session, bool reveal, FrameWorkBudget budget)
    {
        if (!ReferenceEquals(session.Scan?.Graph, Graph))
        {
            return;
        }

        _plan.Tick(Graph, Floor, _spacing, _size, _center, _span, budget);
        EnsureSurface(budget);
        _surface?.Tick(session.Exploration, session.VisitedRoomCoverage, reveal, budget);
    }

    private void EnsureSurface(FrameWorkBudget budget)
    {
        if (_surface == null && _plan.Ready != null && budget.TryStartWork())
        {
            _surface = new FloorSurfaceRenderer(_plan.Ready);
        }
    }

    /// <summary>Changes the displayed variant while preserving ready geometry, masks and images.</summary>
    public void SetMode(bool reveal) => _surface?.SetMode(reveal);

    /// <summary>Discards old visibility-mode pixels while retaining the completed static floor raster.</summary>
    public void InvalidateSurface()
    {
        _surface?.Dispose();
        _surface = null;
    }

    /// <summary>Cancels pending raster/mask work and releases this floor's textures and material.</summary>
    public void Dispose()
    {
        _plan.Dispose();
        InvalidateSurface();
    }
}
