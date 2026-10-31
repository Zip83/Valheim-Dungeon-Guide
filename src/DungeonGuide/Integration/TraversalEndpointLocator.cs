using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Matches a recorded physical endpoint to a close current sample using only indexed columns.</summary>
internal static class TraversalEndpointLocator
{
    /// <summary>Returns a nearby sampled endpoint, or -1 while its geometry is unavailable.</summary>
    public static int Find(ScanResult scan, Point position)
    {
        var best = scan.Spacing * 0.85f + 0.1f;
        var result = -1;
        foreach (var id in Nearby(scan, position))
        {
            var distance = Point.Distance(position, scan.Graph.Nodes[id].Position);
            if (distance < best)
            {
                best = distance;
                result = id;
            }
        }

        return result;
    }

    private static IEnumerable<int> Nearby(ScanResult scan, Point position)
    {
        var x = Mathf.RoundToInt(position.x / scan.Spacing);
        var z = Mathf.RoundToInt(position.z / scan.Spacing);
        for (var dx = -1; dx <= 1; dx++)
        {
            for (var dz = -1; dz <= 1; dz++)
            {
                if (scan.Columns.TryGetValue((x + dx, z + dz), out var nodes))
                {
                    foreach (var id in nodes)
                    {
                        yield return id;
                    }
                }
            }
        }
    }
}
