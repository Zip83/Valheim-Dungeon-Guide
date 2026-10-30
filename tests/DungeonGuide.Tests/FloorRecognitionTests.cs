using System;
using System.Threading;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>
/// Protects architectural levels and broad landing recognition during decomposition of floor analysis.
/// </summary>
public sealed class FloorRecognitionTests
{
    /// <summary>
    /// Separate broad floors retain ascending elevations and their own landing destinations.
    /// </summary>
    [Fact]
    public void SeparatesBroadFloors()
    {
        var graph = new NavGraph();
        var lower = TestSurfaces.Add(graph, 0, 8, 8, 0);
        var upper = TestSurfaces.Add(graph, 8, 8, 8, 0);

        var layout = new FloorDetector(new FloorLandingDetector()).Detect(graph, 1.25f);

        Assert.Equal(new[] { 0f, 8f }, layout.Levels);
        Assert.All(lower, id => Assert.Equal(0, graph.Nodes[id].Floor));
        Assert.All(upper, id => Assert.Equal(1, graph.Nodes[id].Floor));
        Assert.Contains(lower, layout.LandingNodes.Contains);
        Assert.Contains(upper, layout.LandingNodes.Contains);
    }

    /// <summary>
    /// Narrow bridges and ring balconies cannot qualify as broad floor landing destinations.
    /// </summary>
    [Theory]
    [InlineData(3, 20, false)]
    [InlineData(12, 12, true)]
    public void RejectsNarrowSurfaceLandings(int width, int depth, bool ring)
    {
        var graph = new NavGraph();
        var surface = TestSurfaces.Add(graph, 0, width, depth, 0, ring);

        var landings = new FloorLandingDetector().Find(graph, surface, 1.25f);

        Assert.Empty(landings);
    }

    /// <summary>
    /// Disconnected decorations cannot accumulate into an artificial architectural floor.
    /// </summary>
    [Fact]
    public void IgnoresDisconnectedDecorations()
    {
        var graph = new NavGraph();
        TestSurfaces.Add(graph, 0, 8, 8, 0);
        for (var i = 0; i < 100; i++)
        {
            graph.Nodes.Add(new NavNode(new Point(i * 3, 8, 30)));
        }

        var layout = new FloorDetector(new FloorLandingDetector()).Detect(graph, 1.25f);

        Assert.Equal(new[] { 0f }, layout.Levels);
    }

    /// <summary>
    /// An empty graph has neither levels nor landing destinations.
    /// </summary>
    [Fact]
    public void HandlesEmptyGraph()
    {
        var layout = new FloorDetector(new FloorLandingDetector()).Detect(new NavGraph(), 1.25f);

        Assert.Empty(layout.Levels);
        Assert.Empty(layout.LandingNodes);
    }

    /// <summary>
    /// Cancelled analysis exits before changing node floor assignments.
    /// </summary>
    [Fact]
    public void HonorsCancellationBeforePublishingAssignments()
    {
        var graph = new NavGraph();
        var surface = TestSurfaces.Add(graph, 0, 8, 8, 0);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
            new FloorLandingDetector().Find(graph, surface, 1.25f, cancellation.Token));
        Assert.Throws<OperationCanceledException>(() =>
            new FloorDetector(new FloorLandingDetector()).Detect(graph, 1.25f, cancellation.Token));
    }
}
