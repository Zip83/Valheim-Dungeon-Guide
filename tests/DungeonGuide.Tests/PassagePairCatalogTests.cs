using System;
using System.Collections.Generic;
using System.Threading;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects worker preselection against removing supported routes or inventing cross-floor candidates.</summary>
public sealed class PassagePairCatalogTests
{
    /// <summary>Stacked endpoints outside the unchanged slope envelope never reach physics linking.</summary>
    [Fact]
    public void FiltersImpossibleStackedWalks()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point(0, 0, 0)));
        graph.Nodes.Add(new NavNode(new Point(0, 10, 0)));
        graph.Nodes.Add(new NavNode(new Point(1.25f, 0, 0)));
        graph.Nodes.Add(new NavNode(new Point(1.25f, 10, 0)));
        var columns = new Dictionary<(int, int), List<int>>
        {
            [(0, 0)] = new() { 0, 1 },
            [(1, 0)] = new() { 2, 3 }
        };
        var catalog = PassagePairCatalog.Build(graph, columns);
        Assert.Equal(new[] { (0, 2), (1, 3) }, catalog.Walks[(0, 0)]);
        Assert.All(graph.Nodes, node => Assert.Empty(node.Edges));
    }

    /// <summary>Supported gap slopes and directional drops survive preselection even without jump approval.</summary>
    [Fact]
    public void PreservesGapDirectionsAndSupportedSlopes()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point(0, 0, 0)));
        graph.Nodes.Add(new NavNode(new Point(2.5f, -2, 0)));
        var columns = new Dictionary<(int, int), List<int>>
        {
            [(0, 0)] = new() { 0 },
            [(2, 0)] = new() { 1 }
        };
        var catalog = PassagePairCatalog.Build(graph, columns);
        Assert.Equal(new[] { (0, 1) }, catalog.Gaps[(0, 0)]);
        Assert.Equal(new[] { (1, 0) }, catalog.Gaps[(2, 0)]);
    }

    /// <summary>Cancellation aborts unpublished candidate work instead of returning a partial catalog.</summary>
    [Fact]
    public void CancelsBeforePublishingPairs()
    {
        var columns = new Dictionary<(int, int), List<int>> { [(0, 0)] = new() };
        Assert.Throws<OperationCanceledException>(() => PassagePairCatalog.Build(new NavGraph(), columns,
            new CancellationToken(true)));
    }
}
