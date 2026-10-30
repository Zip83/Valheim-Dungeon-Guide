using System.Linq;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects overview visibility, conservative extents, cancellation, and exploration persistence.</summary>
public sealed class OverviewFloorCatalogTests
{
    /// <summary>Unvisited stacked floors must not appear merely because they share the same horizontal position.</summary>
    [Fact]
    public void ListsOnlyExploredFloorsInDescendingOrder()
    {
        var graph = Graph();
        var exploration = new Exploration();
        exploration.Reveal(graph.Nodes[0]);
        exploration.Reveal(graph.Nodes[2]);

        var catalog = Build(graph, exploration, false);

        Assert.Equal(new[] { 2, 0 }, catalog.Floors.Select(floor => floor.Floor));
    }

    /// <summary>Thumbnail framing cannot disclose the extent of an unvisited corridor on a known floor.</summary>
    [Fact]
    public void FramesOnlyKnownPartsOfExploredFloor()
    {
        var graph = Graph();
        graph.Nodes.Add(new NavNode(new Point(100, 0, 200)) { Floor = 0 });
        var exploration = new Exploration();
        exploration.Reveal(graph.Nodes[0]);

        var floor = Assert.Single(Build(graph, exploration, false).Floors);

        Assert.Equal(graph.Nodes[0].Position, floor.Minimum);
        Assert.Equal(graph.Nodes[0].Position, floor.Maximum);
    }

    /// <summary>Full mode includes all scanned floors while preserving both saved keys and revision.</summary>
    [Fact]
    public void FullModeDoesNotModifyExploration()
    {
        var graph = Graph();
        var exploration = new Exploration();
        exploration.Reveal(graph.Nodes[1]);
        var keys = exploration.Export();
        var revision = exploration.Revision;

        Assert.Equal(new[] { 2, 1, 0 }, Build(graph, exploration, true).Floors.Select(floor => floor.Floor));
        Assert.Equal(keys, exploration.Export());
        Assert.Equal(revision, exploration.Revision);
        Assert.Equal(1, Assert.Single(Build(graph, exploration, false).Floors).Floor);
    }

    /// <summary>With no discovery, explored mode must not expose even a floor count or thumbnail.</summary>
    [Fact]
    public void NoDiscoveryProducesEmptyOverview()
    {
        Assert.Empty(Build(Graph(), new Exploration(), false).Floors);
    }

    /// <summary>Saved visited keys reconstruct the same visible floors after re-entry.</summary>
    [Fact]
    public void RestoresVisibleFloorsFromSavedKeys()
    {
        var graph = Graph();
        var exploration = new Exploration();
        exploration.Reveal(graph.Nodes[2]);
        var restored = new Exploration();
        restored.Load(exploration.Export());

        Assert.Equal(2, Assert.Single(Build(graph, restored, false).Floors).Floor);
    }

    /// <summary>Canceling cooperative classification cannot publish a partly classified floor list.</summary>
    [Fact]
    public void CancellationKeepsIncompleteCatalogPrivate()
    {
        var catalog = new OverviewFloorCatalog();
        using (var job = catalog.Build(Graph(), new Exploration(), true).GetEnumerator())
        {
            Assert.True(job.MoveNext());
            Assert.True(job.MoveNext());
            Assert.Empty(catalog.Floors);
        }

        Assert.Empty(catalog.Floors);
    }

    private static OverviewFloorCatalog Build(NavGraph graph, Exploration exploration, bool revealAll)
    {
        var catalog = new OverviewFloorCatalog();
        foreach (var step in catalog.Build(graph, exploration, revealAll))
        {
        }

        return catalog;
    }

    private static NavGraph Graph()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point(0, 0, 0)) { Floor = 0 });
        graph.Nodes.Add(new NavNode(new Point(0, 6, 0)) { Floor = 1 });
        graph.Nodes.Add(new NavNode(new Point(0, 12, 0)) { Floor = 2 });
        return graph;
    }
}
