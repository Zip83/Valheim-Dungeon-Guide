using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Enumerates directed sample pairs between selected neighboring grid columns without materializing them.
/// </summary>
internal static class NeighborColumnPairs
{
    /// <summary>
    /// Visits offsets in order and pairs every source height with every destination height.
    /// Missing columns produce no pairs; reversing an offset is an explicit caller decision.
    /// </summary>
    /// <param name="columns">Horizontal grid index of sampled node IDs; the origin must exist.</param>
    /// <param name="origin">Source grid column whose height samples are paired with neighbors.</param>
    /// <param name="offsets">Directed neighboring-column offsets visited in caller-supplied order.</param>
    /// <returns>Lazy Cartesian pairs of source and destination node IDs, with no inferred reverse direction.</returns>
    public static IEnumerable<(int from, int to)> Enumerate(
        IReadOnlyDictionary<(int x, int z), List<int>> columns,
        (int x, int z) origin,
        IReadOnlyList<(int x, int z)> offsets)
    {
        var source = columns[origin];
        foreach (var offset in offsets)
        {
            if (!columns.TryGetValue((origin.x + offset.x, origin.z + offset.z), out var destination))
            {
                continue;
            }

            foreach (var pair in CrossProduct(source, destination))
            {
                yield return pair;
            }
        }
    }

    private static IEnumerable<(int from, int to)> CrossProduct(List<int> source, List<int> destination)
    {
        foreach (var from in source)
        {
            foreach (var to in destination)
            {
                yield return (from, to);
            }
        }
    }
}
