using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Captures solid door coverage to distinguish moving floor panels from ordinary vertical doors.</summary>
internal sealed class DoorGeometrySnapshot
{
    private readonly Dictionary<int, Bounds> _solids = new();

    /// <summary>Records active collider bounds without querying physics or changing native objects.</summary>
    public DoorGeometrySnapshot(Door door)
    {
        foreach (var collider in door.GetComponentsInChildren<Collider>())
        {
            if (collider.enabled && !collider.isTrigger)
            {
                _solids.Add(collider.GetInstanceID(), collider.bounds);
            }
        }
    }

    /// <summary>Compares coverage with a tolerance so stable colliders do not restart the settling delay.</summary>
    public bool Matches(DoorGeometrySnapshot other) =>
        _solids.Count == other._solids.Count && _solids.All(solid =>
            other._solids.TryGetValue(solid.Key, out var bounds) && SameBounds(solid.Value, bounds));

    /// <summary>Detects changed floor-like colliders without counting stationary thresholds or frames.</summary>
    public bool ChangesFloor(DoorGeometrySnapshot other) =>
        _solids.Any(solid => MovedFloor(solid, other))
        || other._solids.Any(solid => !_solids.ContainsKey(solid.Key) && FloorLike(solid.Value));

    private static bool MovedFloor(KeyValuePair<int, Bounds> solid, DoorGeometrySnapshot other)
    {
        if (!other._solids.TryGetValue(solid.Key, out var bounds))
        {
            return FloorLike(solid.Value);
        }

        return !SameBounds(solid.Value, bounds) && (FloorLike(solid.Value) || FloorLike(bounds));
    }

    private static bool SameBounds(Bounds a, Bounds b) =>
        (a.center - b.center).sqrMagnitude <= 0.0001f && (a.size - b.size).sqrMagnitude <= 0.0001f;

    private static bool FloorLike(Bounds bounds)
    {
        var width = Mathf.Min(bounds.size.x, bounds.size.z);
        return width >= 0.5f && bounds.size.y <= width * 0.5f;
    }
}
