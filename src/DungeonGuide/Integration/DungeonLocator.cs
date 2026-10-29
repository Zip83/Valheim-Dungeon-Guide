using System;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Finds the loaded dungeon generator containing the player or creates a bounded manual scan region.
/// </summary>
internal sealed class DungeonLocator
{
    /// <summary>
    /// Returns the loaded generated dungeon containing the supplied position, or null outside one.
    /// </summary>
    public DungeonArea Locate(Vector3 position)
    {
        DungeonArea result = null;
        var best = float.MaxValue;
        foreach (var g in UnityEngine.Object.FindObjectsByType<DungeonGenerator>(FindObjectsSortMode.None))
        {
            var rooms = g.GetComponentsInChildren<Room>(true);
            if (rooms.Length == 0)
            {
                continue;
            }

            var boxes = rooms.Select(RoomBounds).ToArray();
            var bounds = boxes[0];
            foreach (var b in boxes)
            {
                bounds.Encapsulate(b);
            }

            bounds.Expand(6);
            var distance = Vector3.Distance(bounds.ClosestPoint(position), position);
            if (distance >= 4 || distance >= best)
            {
                continue;
            }

            best = distance;
            Point? entrance = null;
            var entry = rooms.FirstOrDefault(r => r.m_entrance);
            if (entry)
            {
                var connection = entry.GetConnections()?.FirstOrDefault(c => c && c.m_entrance);
                entrance = UnityPoints.P(connection ? connection.transform.position : entry.transform.position);
            }

            result = new DungeonArea
            {
                Id = "dungeon_" + UnityPoints.PositionId(g.transform.position) + "_" + g.m_themes,
                Label = g.m_themes.ToString(),
                Bounds = bounds,
                Rooms = boxes,
                Entrance = entrance
            };
        }

        return result;
    }

    /// <summary>
    /// Creates a bounded manual scan region around a supplied origin without claiming it is a known entrance.
    /// </summary>
    public DungeonArea Manual(Vector3 p, float radius, float height) => new()
    {
        Id = "fortress_" + Mathf.FloorToInt(p.x / 128) + "_" + Mathf.FloorToInt(p.y / 1000) + "_"
            + Mathf.FloorToInt(p.z / 128),
        Label = GuideText.ManualArea,
        Bounds = new Bounds(p, new Vector3(radius * 2, height * 2, radius * 2)),
        Rooms = Array.Empty<Bounds>()
    };

    private static Bounds RoomBounds(Room r)
    {
        var s = r.m_size;
        var b = new Bounds(r.transform.position, Vector3.zero);
        for (var x = -1; x <= 1; x += 2)
        {
            for (var y = -1; y <= 1; y += 2)
            {
                for (var z = -1; z <= 1; z += 2)
                {
                    b.Encapsulate(r.transform.TransformPoint(new Vector3(s.x * x, s.y * y, s.z * z) * 0.5f));
                }
            }
        }

        return b;
    }
}
