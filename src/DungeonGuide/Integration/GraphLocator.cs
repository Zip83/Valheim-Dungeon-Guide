using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Snaps a world position to a supported nearby sample in a completed scan.
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
    /// Returns a nearby supported scan node that can be reached from the supplied position, or -1.
    /// </summary>
    public int Snap(ScanResult scan, Vector3 position, float max = 3.2f)
    {
        if (scan == null)
        {
            return -1;
        }

        var result = -1;
        var best = max;
        var x = Mathf.RoundToInt(position.x / scan.Spacing);
        var z = Mathf.RoundToInt(position.z / scan.Spacing);
        var r = Mathf.CeilToInt(max / scan.Spacing);
        for (var dx = -r; dx <= r; dx++)
        {
            for (var dz = -r; dz <= r; dz++)
            {
                if (!scan.Columns.TryGetValue((x + dx, z + dz), out var list))
                {
                    continue;
                }

                foreach (var n in list)
                {
                    var p = UnityPoints.V(scan.Graph.Nodes[n].Position);
                    var d = Vector3.Distance(position, p);
                    if (d < best && Mathf.Abs(position.y - p.y) < 1.8f && _physics.Visible(position, p))
                    {
                        best = d;
                        result = n;
                    }
                }
            }
        }

        return result;
    }
}
