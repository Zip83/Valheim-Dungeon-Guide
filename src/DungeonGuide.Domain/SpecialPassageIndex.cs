using System;
using System.Collections.Generic;
using System.Threading;

namespace DungeonGuide;

/// <summary>Indexes special passage records once per published graph for repeated permission refreshes.</summary>
internal sealed class SpecialPassageIndex
{
    private NavGraph _graph;

    /// <summary>Directed door and destructible passages with their source node indices.</summary>
    public List<(int Source, NavEdge Edge)> Passages { get; } = new();

    /// <summary>Destructible identities already represented by navigation passages.</summary>
    public HashSet<string> BreakablePrefabs { get; } = new(StringComparer.Ordinal);

    /// <summary>Yields per edge and only publishes index readiness after the complete graph was visited.</summary>
    public IEnumerable<byte> Build(NavGraph graph, CancellationToken cancellation = default)
    {
        if (_graph == graph)
        {
            yield break;
        }

        Passages.Clear();
        BreakablePrefabs.Clear();
        for (var source = 0; source < graph.Nodes.Count; source++)
        {
            cancellation.ThrowIfCancellationRequested();
            yield return 0;
            foreach (var edge in graph.Nodes[source].Edges)
            {
                if (edge.Kind == PassageKind.Door || edge.Kind == PassageKind.Breakable)
                {
                    Passages.Add((source, edge));
                    if (edge.Kind == PassageKind.Breakable && edge.Obstacle != null)
                    {
                        BreakablePrefabs.Add(edge.Obstacle);
                    }
                }

                yield return 0;
            }
        }

        _graph = graph;
    }
}
