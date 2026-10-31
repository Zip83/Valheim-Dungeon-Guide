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
    /// <param name="text">Selected-language catalog used for manual scan labels.</param>
    /// <param name="entrances">Confirmed local dungeon arrivals.</param>
    public DungeonLocator(GuideText text, DungeonEntranceRecorder entrances)
    {
        _areas = new DungeonAreaFactory(text, entrances);
    }

    /// <summary>
    /// Returns the nearest containing generated dungeon, or null outside an interior.
    /// </summary>
    /// <param name="position">Player position in world metres.</param>
    /// <returns>Closest containing dungeon descriptor, or null when no generated interior contains the
    /// position.</returns>
    public DungeonArea Locate(Vector3 position)
    {
        return Character.InInterior(position) ? FindClosestArea(position) : null;
    }

    /// <summary>
    /// Delegates explicit manual scan-area creation without claiming a known entrance.
    /// </summary>
    /// <param name="position">Centre of the requested world-space scan volume.</param>
    /// <param name="radius">Horizontal half-extent in metres.</param>
    /// <param name="height">Vertical half-extent in metres.</param>
    /// <returns>Manual area descriptor with no assumed entrance.</returns>
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
