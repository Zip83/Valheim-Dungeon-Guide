using System.Collections.Generic;
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
    /// <param name="scan">Completed geometry and its horizontal column index.</param>
    /// <param name="exploration">Session-owned explored sample set to update.</param>
    /// <param name="player">Player position in world metres.</param>
    public void Track(ScanResult scan, Exploration exploration, Vector3 player)
    {
        foreach (var column in NearbyColumns(scan, player))
        {
            RevealColumn(scan, exploration, column, player);
        }
    }

    private static IEnumerable<List<int>> NearbyColumns(ScanResult scan, Vector3 player)
    {
        var x = Mathf.RoundToInt(player.x / scan.Spacing);
        var z = Mathf.RoundToInt(player.z / scan.Spacing);
        var radius = Mathf.CeilToInt(3 / scan.Spacing);
        for (var dx = -radius; dx <= radius; dx++)
        {
            for (var dz = -radius; dz <= radius; dz++)
            {
                if (!scan.Columns.TryGetValue((x + dx, z + dz), out var column))
                {
                    continue;
                }

                yield return column;
            }
        }
    }

    private void RevealColumn(ScanResult scan, Exploration exploration, List<int> column, Vector3 player)
    {
        foreach (var id in column)
        {
            RevealSample(exploration, scan.Graph.Nodes[id], player);
        }
    }

    private void RevealSample(Exploration exploration, NavNode node, Vector3 player)
    {
        var position = UnityPoints.V(node.Position);
        if (exploration.Has(node) || Mathf.Abs(player.y - position.y) > 0.9f
            || Vector3.Distance(player, position) > 3)
        {
            return;
        }

        if (_physics.Visible(player, position))
        {
            exploration.Reveal(node);
        }
    }
}
