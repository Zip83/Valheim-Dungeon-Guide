using System;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Owns cached overview terrain and incremental exploration and visit masks.</summary>
internal sealed class OverviewThumbnailRenderer : IDisposable
{
    private readonly FloorPlanCache _plan = new();
    private FloorPlanRaster _raster;
    private FloorSurfaceRenderer _surface;
    private FloorSurfaceRenderer _pending;

    /// <summary>Completed surface retained while a replacement is prepared.</summary>
    public Texture2D Texture => _surface?.Texture;

    /// <summary>Owned shader material, or null for CPU compositing.</summary>
    public Material Material => _surface?.Material;

    /// <summary>Advances only dirty mask tiles after initial terrain construction.</summary>
    public void Tick(DungeonSession session, OverviewFloor floor, bool revealAll, FrameWorkBudget budget)
    {
        var center = new Point((floor.Minimum.x + floor.Maximum.x) / 2, 0,
            (floor.Minimum.z + floor.Maximum.z) / 2);
        var span = Mathf.Max(floor.Maximum.x - floor.Minimum.x, floor.Maximum.z - floor.Minimum.z)
            + session.Scan.Spacing * 3;
        _plan.Tick(session.Scan.Graph, floor.Floor, session.Scan.Spacing, OverviewStyle.TextureSize, center, span, budget);
        var raster = _plan.Ready;
        if (raster == null)
        {
            return;
        }

        if (_raster != raster)
        {
            _pending?.Dispose();
            _raster = raster;
            _pending = new FloorSurfaceRenderer(raster);
        }

        var next = _pending ?? _surface;
        next.Tick(session.OverviewExploration, session.VisitedRoomCoverage, revealAll, budget);
        if (_pending?.Ready == true)
        {
            _surface?.Dispose();
            _surface = _pending;
            _pending = null;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _surface?.Dispose();
        _pending?.Dispose();
        _surface = _pending = null;
        _raster = null;
        _plan.Dispose();
    }
}
