using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Admits one background floor fairly after the foreground image is ready.</summary>
internal sealed class FloorPrefetchScheduler
{
    private int _cursor;

    /// <summary>Chooses a nonforeground target only when the existing frame budget has capacity.</summary>
    public bool TrySelect(IReadOnlyList<int> targets, bool foregroundReady, FrameWorkBudget budget, out int floor)
    {
        floor = -1;
        if (!foregroundReady || targets.Count <= 1 || !budget.TryStartWork())
        {
            return false;
        }

        var count = targets.Count - 1;
        var index = _cursor % count;
        _cursor = (index + 1) % count;
        floor = targets[index + 1];
        return true;
    }

    /// <summary>Restarts neighbor ordering when the dungeon session changes.</summary>
    public void Reset() => _cursor = 0;
}
