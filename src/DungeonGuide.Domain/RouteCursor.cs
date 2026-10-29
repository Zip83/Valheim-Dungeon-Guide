using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Locates player progress along an existing route using three-dimensional distance.
/// </summary>
public sealed class RouteCursor
{
    /// <summary>
    /// Returns the route-list position closest to the player, or zero for an empty route.
    /// </summary>
    /// <param name="graph">Sampled floor graph whose node indices are used by the operation.</param>
    /// <param name="route">Ordered graph-node indices belonging to the current route.</param>
    /// <param name="player">Current player position in world metres.</param>
    /// <returns>Position in the route list, rather than a graph-node index.</returns>
    public int Nearest(NavGraph graph, IReadOnlyList<int> route, Point player)
    {
        var result = 0;
        var best = float.MaxValue;
        for (var i = 0; i < route.Count; i++)
        {
            var distance = Point.Distance(player, graph.Nodes[route[i]].Position);
            if (distance < best)
            {
                best = distance;
                result = i;
            }
        }

        return result;
    }
}
