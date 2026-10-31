using System;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Owns one overview projection and retains its last ready image during a cooperative replacement.</summary>
internal sealed class ProjectedOverviewSurface : IDisposable
{
    private readonly FloorPlanCache _plan = new();
    private FloorPlanRaster _candidate;
    private FloorSurfaceRenderer _pending;
    private FloorSurfaceRenderer _surface;

    /// <summary>Whether a completed image is available.</summary>
    public bool Ready => _surface != null;

    /// <summary>Projection paired with the displayed image, not an unfinished replacement.</summary>
    public FloorPlanRaster Raster
    {
        get; private set;
    }

    /// <summary>Mode-selected CPU image.</summary>
    public Texture2D Texture => _surface?.Texture;

    /// <summary>Updates image selection without discarding geometry or pending work.</summary>
    public void SetMode(bool reveal)
    {
        _surface?.SetMode(reveal);
        _pending?.SetMode(reveal);
    }

    /// <summary>Advances this projection within the caller's shared presentation budget.</summary>
    public void Tick(DungeonSession session, OverviewFrame frame, bool reveal, FrameWorkBudget budget)
    {
        _plan.Tick(session.Scan.Graph, frame.Floor, session.Scan.Spacing, OverviewStyle.TextureSize,
            frame.Center, frame.Span, budget);
        if (_plan.Ready == null)
        {
            return;
        }

        EnsureReplacement(_plan.Ready, budget);
        (_pending ?? _surface)?.Tick(session.OverviewExploration, session.VisitedRoomCoverage, reveal, budget);
        PublishReplacement();
    }

    private void EnsureReplacement(FloorPlanRaster raster, FrameWorkBudget budget)
    {
        if (_candidate != raster && budget.TryStartWork())
        {
            _pending?.Dispose();
            _candidate = raster;
            _pending = new FloorSurfaceRenderer(raster);
        }
    }

    private void PublishReplacement()
    {
        if (_pending?.Ready == true)
        {
            _surface?.Dispose();
            _surface = _pending;
            _pending = null;
            Raster = _candidate;
        }
    }

    /// <summary>Cancels pending work and releases both ready and replacement resources.</summary>
    public void Dispose()
    {
        _pending?.Dispose();
        _surface?.Dispose();
        _pending = _surface = null;
        _candidate = Raster = null;
        _plan.Dispose();
    }
}
