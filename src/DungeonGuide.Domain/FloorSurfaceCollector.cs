using System;
using System.Collections.Generic;
using System.Threading;

namespace DungeonGuide;

/// <summary>
/// Collects connected horizontal surfaces with broad interior landing support.
/// </summary>
internal static class FloorSurfaceCollector
{
    internal static List<(float height, int count, List<int> landings)> Collect(
        NavGraph graph, float spacing, int minimum, FloorLandingDetector landingDetector,
        CancellationToken cancellationToken)
    {
        // Count connected, near-horizontal surfaces. Isolated treads and decoration must not
        // accumulate into artificial floors simply because they share the same elevation.
        var surfaces = new List<(float height, int count, List<int> landings)>();
        var seen = new HashSet<int>();
        for (var i = 0; i < graph.Nodes.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!graph.Nodes[i].Flat || !seen.Add(i))
            {
                continue;
            }

            var members = CollectSurface(graph, i, seen, cancellationToken);
            if (members.Count >= minimum)
            {
                var landings = landingDetector.Find(graph, members, spacing, cancellationToken);
                if (landings.Count >= 4)
                {
                    surfaces.Add((AverageHeight(graph, members, cancellationToken), members.Count, landings));
                }
            }
        }

        return surfaces;
    }

    private static List<int> CollectSurface(
        NavGraph graph, int start, HashSet<int> seen, CancellationToken cancellationToken)
    {
        var reference = graph.Nodes[start].Position.y;
        var members = new List<int>();
        var queue = new Queue<int>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var n = queue.Dequeue();
            members.Add(n);
            foreach (var edge in graph.Nodes[n].Edges)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var next = graph.Nodes[edge.To];
                if (edge.Verified && (edge.Kind == PassageKind.Walk || edge.Kind == PassageKind.Stairs) && next.Flat
                    && Math.Abs(next.Position.y - reference) < 0.35f
                    && seen.Add(edge.To))
                {
                    queue.Enqueue(edge.To);
                }
            }
        }

        return members;
    }

    private static float AverageHeight(NavGraph graph, List<int> members, CancellationToken cancellationToken)
    {
        float sum = 0;
        foreach (var id in members)
        {
            cancellationToken.ThrowIfCancellationRequested();
            sum += graph.Nodes[id].Position.y;
        }

        return sum / members.Count;
    }
}
