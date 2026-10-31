using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Orders every dungeon floor while distinguishing the immediate physical neighborhood.</summary>
internal static class FloorPreloadPolicy
{
    /// <summary>Orders the viewed floor, physical neighborhood and then increasingly distant floors.</summary>
    /// <param name="target">Reusable list receiving every existing floor once when a valid anchor exists.</param>
    /// <param name="physicalFloor">Player floor; invalid values do not imply physical neighbors.</param>
    /// <param name="selectedFloor">Manually browsed or automatically followed display floor.</param>
    /// <param name="floorCount">Number of available ascending architectural floors.</param>
    /// <returns>Length of the foreground/neighborhood prefix that must be prepared before distant floors.</returns>
    public static int Fill(List<int> target, int physicalFloor, int selectedFloor, int floorCount)
    {
        target.Clear();
        AddValid(target, selectedFloor, floorCount);
        var physicalValid = physicalFloor >= 0 && physicalFloor < floorCount;
        if (physicalValid)
        {
            AddNeighborhood(target, physicalFloor, floorCount);
        }

        var priorityCount = target.Count;
        if (priorityCount > 0)
        {
            AddRemaining(target, physicalValid ? physicalFloor : selectedFloor, floorCount);
        }

        return priorityCount;
    }

    private static void AddNeighborhood(List<int> target, int floor, int count)
    {
        AddValid(target, floor, count);
        AddValid(target, floor + 1, count);
        AddValid(target, floor - 1, count);
    }

    private static void AddRemaining(List<int> target, int center, int count)
    {
        for (var distance = 1; distance < count; distance++)
        {
            AddValid(target, center + distance, count);
            AddValid(target, center - distance, count);
        }
    }

    private static void AddValid(List<int> target, int floor, int count)
    {
        if (floor >= 0 && floor < count && !target.Contains(floor))
        {
            target.Add(floor);
        }
    }
}
