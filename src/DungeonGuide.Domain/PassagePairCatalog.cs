using System;
using System.Collections.Generic;
using System.Threading;

namespace DungeonGuide;

/// <summary>Preselects geometrically possible passage pairs without querying Unity or mutating a graph.</summary>
internal sealed class PassagePairCatalog
{
    private static readonly (int, int)[] s_walk = { (1, 0), (0, 1), (1, 1), (1, -1) };
    private static readonly (int, int)[] s_gap = { (2, 0), (0, 2), (-2, 0), (0, -2) };

    /// <summary>Ordinary candidates grouped in original column and endpoint order.</summary>
    public Dictionary<(int, int), List<(int from, int to)>> Walks { get; } = new();

    /// <summary>Supported or airborne gap candidates retaining their original directions.</summary>
    public Dictionary<(int, int), List<(int from, int to)>> Gaps { get; } = new();

    /// <summary>Reads exclusively owned sampled geometry on a worker; applies only existing endpoint envelopes.</summary>
    public static PassagePairCatalog Build(NavGraph graph, IReadOnlyDictionary<(int, int), List<int>> columns,
        CancellationToken cancellation = default)
    {
        var result = new PassagePairCatalog();
        foreach (var column in columns.Keys)
        {
            cancellation.ThrowIfCancellationRequested();
            result.Walks[column] = Select(graph, columns, column, s_walk, false, cancellation);
            result.Gaps[column] = Select(graph, columns, column, s_gap, true, cancellation);
        }

        return result;
    }

    private static List<(int from, int to)> Select(NavGraph graph,
        IReadOnlyDictionary<(int, int), List<int>> columns, (int, int) column,
        IReadOnlyList<(int, int)> offsets, bool includeJumps, CancellationToken cancellation)
    {
        var result = new List<(int from, int to)>();
        foreach (var pair in NeighborColumnPairs.Enumerate(columns, column, offsets))
        {
            cancellation.ThrowIfCancellationRequested();
            var a = graph.Nodes[pair.from].Position;
            var b = graph.Nodes[pair.to].Position;
            var dx = b.x - a.x;
            var dz = b.z - a.z;
            var horizontal = (float)Math.Sqrt(dx * dx + dz * dz);
            var rise = b.y - a.y;
            if (Math.Abs(rise) <= horizontal * 0.85f + 0.18f
                || includeJumps && JumpCandidateGeometry.WithinRange(horizontal, rise))
            {
                result.Add(pair);
            }
        }

        return result;
    }
}
