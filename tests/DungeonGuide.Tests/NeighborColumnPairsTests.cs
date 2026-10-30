using System.Collections.Generic;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>
/// Protects stacked-floor pairing, directional offsets, and lazy neighbor lookup during scan linking.
/// </summary>
public sealed class NeighborColumnPairsTests
{
    /// <summary>
    /// Every sampled source height is checked against every neighboring height in the original order.
    /// </summary>
    [Fact]
    public void PairsAllStackedHeights()
    {
        var columns = new Dictionary<(int, int), List<int>>
        {
            [(0, 0)] = new() { 0, 1 },
            [(1, 0)] = new() { 2, 3 }
        };

        var pairs = NeighborColumnPairs.Enumerate(columns, (0, 0), new[] { (1, 0) });

        Assert.Equal(new[] { (0, 2), (0, 3), (1, 2), (1, 3) }, pairs);
    }

    /// <summary>
    /// Missing neighbor columns do not introduce connections to unrelated nearby samples.
    /// </summary>
    [Fact]
    public void SkipsMissingColumns()
    {
        var columns = new Dictionary<(int, int), List<int>>
        {
            [(0, 0)] = new() { 0 },
            [(2, 0)] = new() { 1 }
        };

        var pairs = NeighborColumnPairs.Enumerate(columns, (0, 0), new[] { (1, 0), (0, 1) });

        Assert.Empty(pairs);
    }

    /// <summary>
    /// A directed offset does not invent reverse pairs, and caller-supplied offset order is retained.
    /// </summary>
    [Fact]
    public void PreservesExplicitDirectionsAndOffsetOrder()
    {
        var columns = new Dictionary<(int, int), List<int>>
        {
            [(0, 0)] = new() { 0 },
            [(2, 0)] = new() { 1 },
            [(0, 2)] = new() { 2 },
            [(-2, 0)] = new() { 3 }
        };

        var pairs = NeighborColumnPairs.Enumerate(columns, (0, 0), new[] { (0, 2), (2, 0) });

        Assert.Equal(new[] { (0, 2), (0, 1) }, pairs);
    }

    /// <summary>
    /// Later neighbor columns are not copied or visited before the caller advances enumeration.
    /// </summary>
    [Fact]
    public void LooksUpLaterNeighborsLazily()
    {
        var columns = new Dictionary<(int, int), List<int>>
        {
            [(0, 0)] = new() { 0 },
            [(1, 0)] = new() { 1 }
        };
        using var pairs = NeighborColumnPairs.Enumerate(columns, (0, 0), new[] { (1, 0), (0, 1) }).GetEnumerator();
        Assert.True(pairs.MoveNext());
        Assert.Equal((0, 1), pairs.Current);

        columns[(0, 1)] = new List<int> { 2 };

        Assert.True(pairs.MoveNext());
        Assert.Equal((0, 2), pairs.Current);
        Assert.False(pairs.MoveNext());
    }
}
