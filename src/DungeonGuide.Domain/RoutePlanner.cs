using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Finds a least-cost route through verified passages allowed by the current traversal policy.
/// </summary>
public sealed class RoutePlanner
{
    /// <summary>
    /// Returns ordered node indices to the first least-cost matching destination, or an empty route when unreachable.
    /// </summary>
    /// <param name="graph">Sampled floor graph whose node indices are used by the operation.</param>
    /// <param name="start">Starting graph-node index.</param>
    /// <param name="target">Predicate identifying acceptable destination node indices.</param>
    /// <param name="policy">Traversal permissions and special-passage cost preferences.</param>
    /// <returns>Ordered route including the start and destination, or an empty list if unreachable.</returns>
    public List<int> Find(NavGraph graph, int start, Predicate<int> target, RoutePolicy policy)
    {
        if (start < 0 || start >= graph.Nodes.Count)
        {
            return new List<int>();
        }

        var distance = new float[graph.Nodes.Count];
        var parent = new int[distance.Length];
        for (var i = 0; i < distance.Length; i++)
        {
            distance[i] = float.PositiveInfinity;
            parent[i] = -1;
        }

        var queue = new SortedSet<(float, int)>();
        distance[start] = 0;
        queue.Add((0, start));
        while (queue.Count > 0)
        {
            var c = queue.Min;
            queue.Remove(c);
            if (target(c.Item2))
            {
                return ReconstructRoute(parent, c.Item2);
            }

            RelaxEdges(graph, c, policy, distance, parent, queue);
        }

        return new List<int>();
    }

    private static List<int> ReconstructRoute(int[] parent, int destination)
    {
        var route = new List<int>();
        var n = destination;
        while (n != -1)
        {
            route.Add(n);
            n = parent[n];
        }

        route.Reverse();
        return route;
    }

    private static void RelaxEdges(
        NavGraph graph,
        (float, int) c,
        RoutePolicy policy,
        float[] distance,
        int[] parent,
        SortedSet<(float, int)> queue)
    {
        foreach (var edge in graph.Nodes[c.Item2].Edges)
        {
            if (!policy.Allows(edge))
            {
                continue;
            }

            var next = edge.To;
            var cost = c.Item1 + Point.Distance(graph.Nodes[c.Item2].Position, graph.Nodes[next].Position)
                + policy.Penalty(edge);
            if (cost >= distance[next])
            {
                continue;
            }

            queue.Remove((distance[next], next));
            distance[next] = cost;
            parent[next] = c.Item2;
            queue.Add((cost, next));
        }
    }
}
