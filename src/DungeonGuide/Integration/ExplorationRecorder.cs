using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Reveals nearby visible floor samples from the player position.
/// </summary>
internal sealed class ExplorationRecorder
{
    private readonly GeometryPhysics _physics;
    private readonly Func<Point, Point, bool> _visible;

    /// <summary>
    /// Uses collision visibility to reveal nearby floor samples.
    /// </summary>
    public ExplorationRecorder(GeometryPhysics physics)
    {
        _physics = physics;
        _visible = Visible;
    }

    /// <summary>
    /// Reveals nearby samples only when visibility and vertical proximity checks succeed.
    /// </summary>
    /// <param name="scan">Completed geometry and its horizontal column index.</param>
    /// <param name="exploration">Session-owned explored sample set to update.</param>
    /// <param name="player">Player position in world metres.</param>
    /// <param name="physicalFloor">Detected player floor, independent of the browsed map floor.</param>
    public void Track(ScanResult scan, Exploration exploration, Vector3 player, int physicalFloor)
    {
        var position = UnityPoints.P(player);
        foreach (var column in NearbyColumns(scan, player))
        {
            RevealColumn(scan, exploration, column, position, physicalFloor);
        }
    }

    private bool Visible(Point a, Point b) => _physics.Visible(UnityPoints.V(a), UnityPoints.V(b));

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

    private void RevealColumn(
        ScanResult scan,
        Exploration exploration,
        List<int> column,
        Point player,
        int physicalFloor)
    {
        foreach (var id in column)
        {
            ExplorationRevealPolicy.TryReveal(exploration, scan.Graph.Nodes[id], player, physicalFloor, _visible);
        }
    }
}
