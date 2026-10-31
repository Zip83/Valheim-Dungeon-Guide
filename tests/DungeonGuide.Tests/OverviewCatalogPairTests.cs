using System.Linq;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects instant mode metadata selection without revealing unknown floors or extents.</summary>
public sealed class OverviewCatalogPairTests
{
    /// <summary>Repeated switches select existing lists and never modify exploration or rebuild metadata.</summary>
    [Fact]
    public void ModeSelectionReusesPublishedLists()
    {
        var graph = Graph();
        var exploration = new Exploration();
        exploration.Reveal(graph.Nodes[0]);
        var pair = Build(graph, exploration);
        var full = pair.Visible(true);
        var known = pair.Visible(false);
        var revision = pair.Revision;
        var saved = exploration.Export();
        for (var i = 0; i < 100; i++)
        {
            Assert.Same(full, pair.Visible(true));
            Assert.Same(known, pair.Visible(false));
        }

        Assert.Equal(revision, pair.Revision);
        Assert.Equal(saved, exploration.Export());
        Assert.Equal(new[] { 1, 0 }, full.Select(floor => floor.Floor));
        Assert.Equal(0, Assert.Single(known).Floor);
    }

    /// <summary>Known framing remains limited to discovered positions even with the full view ready.</summary>
    [Fact]
    public void FullCacheDoesNotExpandKnownExtent()
    {
        var graph = Graph();
        var exploration = new Exploration();
        exploration.Reveal(graph.Nodes[0]);
        var pair = Build(graph, exploration);

        Assert.Equal(graph.Nodes[0].Position, pair.KnownFloor(0).Maximum);
        Assert.Equal(graph.Nodes[1].Position, pair.FullFloor(0).Maximum);
        Assert.Null(pair.KnownFloor(1));
    }

    /// <summary>Discovery arriving while Full is active is available in the next known view.</summary>
    [Fact]
    public void UpdatesKnownMetadataWithoutTouchingFullList()
    {
        var graph = Graph();
        var exploration = new Exploration();
        var pair = Build(graph, exploration);
        var full = pair.Visible(true);
        exploration.Reveal(graph.Nodes[2]);
        pair.Update(exploration).ToList();

        Assert.Same(full, pair.Visible(true));
        Assert.Equal(1, Assert.Single(pair.Visible(false)).Floor);
    }

    /// <summary>Canceling a partially built pair exposes neither an incomplete known nor full list.</summary>
    [Fact]
    public void IncompletePairStaysPrivate()
    {
        var pair = new OverviewCatalogPair();
        using (var job = pair.Build(Graph(), new Exploration()).GetEnumerator())
        {
            Assert.True(job.MoveNext());
            Assert.Empty(pair.Visible(true));
            Assert.Empty(pair.Visible(false));
        }

        Assert.False(pair.Ready);
    }

    /// <summary>Replacing saved exploration removes old known floors instead of preserving an expanding catalog.</summary>
    [Fact]
    public void ReloadReplacesKnownMetadata()
    {
        var graph = Graph();
        var exploration = new Exploration();
        exploration.Reveal(graph.Nodes[2]);
        var pair = Build(graph, exploration);
        exploration.Load(new[] { graph.Nodes[0].Key });
        pair.Update(exploration).ToList();

        Assert.Equal(0, Assert.Single(pair.Visible(false)).Floor);
        Assert.Equal(2, pair.Visible(true).Count);
    }

    /// <summary>Reloading saved keys during an incremental yield cancels stale change-list access.</summary>
    [Fact]
    public void ReloadDuringUpdateCancelsOldChanges()
    {
        var graph = Graph();
        var exploration = new Exploration();
        exploration.Reveal(graph.Nodes[0]);
        var pair = Build(graph, exploration);
        exploration.Reveal(graph.Nodes[1]);
        using (var job = pair.Update(exploration).GetEnumerator())
        {
            Assert.True(job.MoveNext());
            exploration.Load(new[] { graph.Nodes[2].Key });
            Assert.False(job.MoveNext());
        }

        Assert.False(pair.Ready);
        Assert.Empty(pair.Visible(false));
        pair.Update(exploration).ToList();
        Assert.Equal(1, Assert.Single(pair.Visible(false)).Floor);
    }

    /// <summary>A load while initial classification is paused cannot publish a mixture of saved states.</summary>
    [Fact]
    public void ReloadDuringBuildRequiresFreshKnownCatalog()
    {
        var graph = Graph();
        var exploration = new Exploration();
        exploration.Reveal(graph.Nodes[0]);
        var pair = new OverviewCatalogPair();
        using (var job = pair.Build(graph, exploration).GetEnumerator())
        {
            Assert.True(job.MoveNext());
            exploration.Load(new[] { graph.Nodes[2].Key });
            while (job.MoveNext())
            {
            }
        }

        Assert.False(pair.Ready);
        pair.Update(exploration).ToList();
        Assert.Equal(1, Assert.Single(pair.Visible(false)).Floor);
    }

    private static OverviewCatalogPair Build(NavGraph graph, Exploration exploration)
    {
        var pair = new OverviewCatalogPair();
        pair.Build(graph, exploration).ToList();
        return pair;
    }

    private static NavGraph Graph()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point(0, 0, 0)));
        graph.Nodes.Add(new NavNode(new Point(20, 0, 10)));
        graph.Nodes.Add(new NavNode(new Point(0, 6, 0)) { Floor = 1 });
        return graph;
    }
}
