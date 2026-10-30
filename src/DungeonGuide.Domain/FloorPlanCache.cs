using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Replaces a completed floor raster only after a bounded, cancellable rebuild finishes.
/// </summary>
internal sealed class FloorPlanCache : IDisposable
{
    private NavGraph _graph;
    private int _floor = -1;
    private int _size;
    private float _spacing;
    private float _span;
    private Point _center;
    private FloorPlanRaster _pending;
    private IEnumerator<byte> _build;

    /// <summary>The latest fully constructed grid, retained during a replacement build.</summary>
    public FloorPlanRaster Ready
    {
        get; private set;
    }

    /// <summary>Advances the requested floor without exceeding the caller's shared soft budget.</summary>
    public void Tick(NavGraph graph, int floor, float spacing, int size, Point center, float span, FrameWorkBudget budget)
    {
        if (_graph != graph || _floor != floor || _size != size || _spacing != spacing || _span != span
            || _center.x != center.x || _center.z != center.z)
        {
            _build?.Dispose();
            _graph = graph;
            _floor = floor;
            _size = size;
            _spacing = spacing;
            _span = span;
            _center = center;
            _pending = new FloorPlanRaster(size, floor, center, span);
            _build = new FloorPlanBuilder().Build(graph, spacing, _pending).GetEnumerator();
        }

        while (_build != null && budget.TryStartWork())
        {
            if (!_build.MoveNext())
            {
                _build.Dispose();
                _build = null;
                Ready = _pending;
                _pending = null;
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _build?.Dispose();
        _build = null;
        _graph = null;
        _pending = Ready = null;
        _floor = -1;
    }
}
