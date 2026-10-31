using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Prioritizes neighbor warmup, then shares background slices between distant warmup and ready floors.</summary>
internal sealed class FloorPrefetchScheduler
{
    private int _priorityCursor;
    private int _refreshCursor;
    private bool _refreshNext;

    /// <summary>Admits one background slice without resetting the foreground's shared frame budget.</summary>
    /// <param name="targets">Foreground first, followed by priority neighbors and ordered distant floors.</param>
    /// <param name="priorityCount">End of the immediate-neighborhood prefix, including foreground.</param>
    /// <param name="foregroundReady">Whether the displayed floor has its first complete image.</param>
    /// <param name="isReady">Read-only readiness lookup; never creates a resource.</param>
    /// <param name="budget">Existing presentation budget after foreground work.</param>
    /// <param name="floor">Admitted floor, or -1 when no background work can run.</param>
    /// <returns>Whether a background floor was admitted.</returns>
    public bool TrySelect(IReadOnlyList<int> targets, int priorityCount, bool foregroundReady,
        Func<int, bool> isReady, FrameWorkBudget budget, out int floor)
    {
        floor = -1;
        if (!foregroundReady || targets.Count <= 1 || !budget.TryStartWork())
        {
            return false;
        }

        var priorityEnd = Math.Min(targets.Count, Math.Max(1, priorityCount));
        if (TryPriority(targets, priorityEnd, isReady, out floor))
        {
            return true;
        }

        return TryDistantOrRefresh(targets, priorityEnd, isReady, out floor);
    }

    private bool TryPriority(IReadOnlyList<int> targets, int end, Func<int, bool> isReady, out int floor)
    {
        var count = end - 1;
        for (var offset = 0; offset < count; offset++)
        {
            var index = (_priorityCursor + offset) % count;
            if (!isReady(targets[index + 1]))
            {
                _priorityCursor = (index + 1) % count;
                floor = targets[index + 1];
                return true;
            }
        }

        floor = -1;
        return false;
    }

    private bool TryDistantOrRefresh(IReadOnlyList<int> targets, int start, Func<int, bool> isReady, out int floor)
    {
        if (_refreshNext && TryRefresh(targets, isReady, out floor))
        {
            _refreshNext = false;
            return true;
        }

        if (TryDistant(targets, start, isReady, out floor))
        {
            _refreshNext = true;
            return true;
        }

        return TryRefresh(targets, isReady, out floor);
    }

    private static bool TryDistant(IReadOnlyList<int> targets, int start, Func<int, bool> isReady, out int floor)
    {
        for (var index = start; index < targets.Count; index++)
        {
            if (!isReady(targets[index]))
            {
                floor = targets[index];
                return true;
            }
        }

        floor = -1;
        return false;
    }

    private bool TryRefresh(IReadOnlyList<int> targets, Func<int, bool> isReady, out int floor)
    {
        var count = targets.Count - 1;
        for (var offset = 0; offset < count; offset++)
        {
            var index = (_refreshCursor + offset) % count;
            if (isReady(targets[index + 1]))
            {
                _refreshCursor = (index + 1) % count;
                floor = targets[index + 1];
                return true;
            }
        }

        floor = -1;
        return false;
    }

    /// <summary>Restarts ordering after a floor-selection or dungeon-session change.</summary>
    public void Reset()
    {
        _priorityCursor = _refreshCursor = 0;
        _refreshNext = false;
    }
}
