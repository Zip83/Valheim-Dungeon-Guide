using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects map previews and shared physics scheduling independently of route readiness.</summary>
public sealed class MapPreviewReadinessTests
{
    /// <summary>A local preview needs no navigation edges and excludes stacked geometry.</summary>
    [Fact]
    public void ClassifiesPreviewWithoutNavigation()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point(0, 0, 0)));
        graph.Nodes.Add(new NavNode(new Point(1, 0.5f, 0)));
        graph.Nodes.Add(new NavNode(new Point(0, 4, 0)));
        var layout = PreviewFloorClassifier.Classify(graph, new Point(0, 0.1f, 0));
        Assert.Single(layout.Levels);
        Assert.Equal(0, graph.Nodes[1].Floor);
        Assert.Equal(-1, graph.Nodes[2].Floor);
        Assert.All(graph.Nodes, node => Assert.Empty(node.Edges));
    }

    /// <summary>A preview with no nearby standable sample cannot invent a floor.</summary>
    [Fact]
    public void RejectsUnsupportedPreviewElevation()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point(0, 20, 0)));
        Assert.Empty(PreviewFloorClassifier.Classify(graph, new Point()).Levels);
    }

    /// <summary>Consumers cannot reset the shared frame budget or charge unrelated game processing.</summary>
    [Fact]
    public void SharesActiveWorkAcrossConsumers()
    {
        var now = 0.0;
        var frame = 1;
        var budget = new FrameWorkBudget(100, 1, () => now, () => frame);
        Assert.True(budget.TryStartWork());
        now = 0.6;
        budget.Pause();
        now = 100;
        budget.BeginFrame();
        budget.Resume();
        Assert.True(budget.TryStartWork());
        now += 0.5;
        Assert.False(budget.TryStartWork());
        frame++;
        budget.BeginFrame();
        Assert.True(budget.TryStartWork());
    }

}
