using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Snaps a world position to a visible nearby sample in a completed scan.
/// </summary>
internal sealed class GraphLocator
{
    private readonly GeometryPhysics _physics;

    /// <summary>
    /// Uses solid-geometry checks while snapping a player or chest to sampled floor geometry.
    /// </summary>
    public GraphLocator(GeometryPhysics physics)
    {
        _physics = physics;
    }

    /// <summary>
    /// Returns the closest reachable sample within the distance limit, or -1.
    /// </summary>
    public int Snap(ScanResult scan, Vector3 position, float max = 3.2f)
    {
        if (scan == null)
        {
            return -1;
        }

        var result = -1;
        var best = max;
        foreach (var id in NearbyNodes(scan, position, max))
        {
            var point = UnityPoints.V(scan.Graph.Nodes[id].Position);
            if (TryReachableDistance(position, point, best, out var distance))
            {
                best = distance;
                result = id;
            }
        }

        return result;
    }

    private static IEnumerable<int> NearbyNodes(ScanResult scan, Vector3 position, float distance)
    {
        var x = Mathf.RoundToInt(position.x / scan.Spacing);
        var z = Mathf.RoundToInt(position.z / scan.Spacing);
        var radius = Mathf.CeilToInt(distance / scan.Spacing);
        for (var dx = -radius; dx <= radius; dx++)
        {
            for (var dz = -radius; dz <= radius; dz++)
            {
                if (!scan.Columns.TryGetValue((x + dx, z + dz), out var column))
                {
                    continue;
                }

                foreach (var id in column)
                {
                    yield return id;
                }
            }
        }
    }

    private bool TryReachableDistance(Vector3 origin, Vector3 sample, float limit, out float distance)
    {
        distance = Vector3.Distance(origin, sample);
        return distance < limit && Mathf.Abs(origin.y - sample.y) < 1.8f && _physics.Visible(origin, sample);
    }
}
