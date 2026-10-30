using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Reveals nearby visible floor samples from the player position.
/// </summary>
internal sealed class ExplorationRecorder
{
    private readonly GeometryPhysics _physics;

    /// <summary>
    /// Uses collision visibility to reveal nearby floor samples.
    /// </summary>
    public ExplorationRecorder(GeometryPhysics physics)
    {
        _physics = physics;
    }

    /// <summary>
    /// Reveals nearby samples only when visibility and vertical proximity checks succeed.
    /// </summary>
    public void Track(ScanResult scan, Exploration exploration, Vector3 player)
    {
        var x = Mathf.RoundToInt(player.x / scan.Spacing);
        var z = Mathf.RoundToInt(player.z / scan.Spacing);
        var radius = Mathf.CeilToInt(3 / scan.Spacing);
        for (var dx = -radius; dx <= radius; dx++)
        {
            for (var dz = -radius; dz <= radius; dz++)
            {
                if (scan.Columns.TryGetValue((x + dx, z + dz), out var list))
                {
                    foreach (var id in list)
                    {
                        var p = UnityPoints.V(scan.Graph.Nodes[id].Position);
                        if (exploration.Has(scan.Graph.Nodes[id]) || Mathf.Abs(player.y - p.y) > 0.9f
                            || Vector3.Distance(player, p) > 3)
                        {
                            continue;
                        }

                        if (_physics.Visible(player, p))
                        {
                            exploration.Reveal(scan.Graph.Nodes[id]);
                        }
                    }
                }
            }
        }
    }
}
