using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Enumerates nearby spatial-index columns once each, with no repeated interior-ring scans.</summary>
internal static class GrappleLandingColumns
{
    /// <summary>Returns center-first square rings within the hook's horizontal search envelope.</summary>
    public static IEnumerable<(int, int)> Nearby(Vector3 origin, float range, float spacing)
    {
        var x = Mathf.RoundToInt(origin.x / spacing);
        var z = Mathf.RoundToInt(origin.z / spacing);
        var radius = Mathf.CeilToInt(range / spacing);
        yield return (x, z);
        for (var ring = 1; ring <= radius; ring++)
        {
            for (var dx = -ring; dx <= ring; dx++)
            {
                yield return (x + dx, z - ring);
                yield return (x + dx, z + ring);
            }

            for (var dz = -ring + 1; dz < ring; dz++)
            {
                yield return (x - ring, z + dz);
                yield return (x + ring, z + dz);
            }
        }
    }
}
