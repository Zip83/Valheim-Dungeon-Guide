using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Observes changed dungeon doors until their animated floor coverage has settled.</summary>
internal sealed class DoorGeometryMonitor
{
    private readonly List<DoorGeometryObservation> _pending = new();
    private readonly Action _geometryChanged;
    private float _nextCheck;

    /// <summary>Requests the existing bounded geometry replacement only for changed floor coverage.</summary>
    public DoorGeometryMonitor(Action geometryChanged) => _geometryChanged = geometryChanged;

    /// <summary>Starts or extends observation of a native state change, including replicated changes.</summary>
    public void Observe(Door door, float now)
    {
        foreach (var observation in _pending)
        {
            if (observation.Door == door)
            {
                observation.Restart(now);
                return;
            }
        }

        _pending.Add(new DoorGeometryObservation(door, now));
    }

    /// <summary>Polls pending doors at ten hertz and coalesces settled floor changes into one invalidation.</summary>
    public void Tick(float now)
    {
        if (_pending.Count == 0 || now < _nextCheck)
        {
            return;
        }

        _nextCheck = now + 0.1f;
        if (CompleteSettled(now))
        {
            _geometryChanged();
        }
    }

    private bool CompleteSettled(float now)
    {
        var changed = false;
        for (var i = _pending.Count - 1; i >= 0; i--)
        {
            var observation = _pending[i];
            if (!observation.Door)
            {
                _pending.RemoveAt(i);
            }
            else if (observation.Settled(now))
            {
                changed |= observation.FloorChanged;
                _pending.RemoveAt(i);
            }
        }

        return changed;
    }

    /// <summary>Discards observations from the previous dungeon or world.</summary>
    public void Reset()
    {
        _pending.Clear();
        _nextCheck = 0;
    }
}
