using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Keeps fog paired with its raster and lets in-flight exploration updates finish without starvation.</summary>
internal sealed class FloorFogCache : IDisposable
{
    private FloorPlanRaster _raster;
    private int _revision = -1;
    private FloorFogMask _pending;
    private IEnumerator<byte> _build;

    /// <summary>The most recently completed mask; callers must verify its raster before using it.</summary>
    public FloorFogMask Ready
    {
        get; private set;
    }

    /// <summary>Advances fog only when geometry or saved exploration changes.</summary>
    public void Tick(FloorPlanRaster raster, Exploration exploration, FrameWorkBudget budget)
    {
        if (_raster != raster || _build == null && _revision != exploration.Revision)
        {
            _build?.Dispose();
            _raster = raster;
            _revision = exploration.Revision;
            _pending = new FloorFogMask(raster);
            _build = _pending.Build(exploration).GetEnumerator();
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
        _raster = null;
        _pending = Ready = null;
        _revision = -1;
    }
}
