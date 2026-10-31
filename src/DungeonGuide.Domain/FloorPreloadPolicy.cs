using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Selects a bounded floor working set without requesting nonexistent neighbors.</summary>
internal static class FloorPreloadPolicy
{
    /// <summary>Places the selected floor first, followed by the physical floor and its valid neighbors.</summary>
    /// <param name="target">Reusable list receiving at most four distinct valid floor indices.</param>
    /// <param name="physicalFloor">Player floor; invalid values do not imply any neighbors.</param>
    /// <param name="selectedFloor">Manually browsed or automatically followed display floor.</param>
    /// <param name="floorCount">Number of available ascending architectural floors.</param>
    public static void Fill(List<int> target, int physicalFloor, int selectedFloor, int floorCount)
    {
        target.Clear();
        AddValid(target, selectedFloor, floorCount);
        if (physicalFloor < 0 || physicalFloor >= floorCount)
        {
            return;
        }

        AddValid(target, physicalFloor, floorCount);
        AddValid(target, physicalFloor + 1, floorCount);
        AddValid(target, physicalFloor - 1, floorCount);
    }

    private static void AddValid(List<int> target, int floor, int count)
    {
        if (floor >= 0 && floor < count && !target.Contains(floor))
        {
            target.Add(floor);
        }
    }
}
