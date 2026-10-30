using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Selects the loaded generated dungeon containing the player.
/// </summary>
internal sealed class DungeonLocator
{
    private readonly DungeonAreaFactory _areas;

    /// <summary>
    /// Uses the selected-language catalog when delegating manual scan-area creation.
    /// </summary>
    public DungeonLocator(GuideText text)
    {
        _areas = new DungeonAreaFactory(text);
    }

    /// <summary>
    /// Returns the nearest containing generated dungeon, or null outside an interior.
    /// </summary>
    public DungeonArea Locate(Vector3 position)
    {
        return Character.InInterior(position) ? FindClosestArea(position) : null;
    }

    /// <summary>
    /// Delegates explicit manual scan-area creation without claiming a known entrance.
    /// </summary>
    public DungeonArea Manual(Vector3 position, float radius, float height)
    {
        return _areas.Manual(position, radius, height);
    }

    private DungeonArea FindClosestArea(Vector3 position)
    {
        DungeonArea closest = null;
        var bestDistance = float.MaxValue;
        foreach (var generator in Object.FindObjectsByType<DungeonGenerator>(FindObjectsSortMode.None))
        {
            var area = _areas.Generated(generator, position);
            if (area == null)
            {
                continue;
            }

            var distance = Vector3.Distance(area.Bounds.ClosestPoint(position), position);
            if (distance < 4 && distance < bestDistance)
            {
                closest = area;
                bestDistance = distance;
            }
        }

        return closest;
    }
}
