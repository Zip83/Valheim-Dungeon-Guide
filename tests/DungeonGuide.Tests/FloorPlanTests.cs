using System.Linq;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects conservative floor coverage, outlines, and cooperative cache publication.</summary>
public sealed class FloorPlanTests
{
    /// <summary>Verified cardinal walking produces a continuous floor rather than separated sample squares.</summary>
    [Fact]
    public void JoinsVerifiedWalkingSamples()
    {
        var graph = Pair();
        graph.Connect(0, 1);
        var raster = Build(graph);

        Assert.True(raster.NodeAt(new Point(1, 0, 0)) >= 0);
        Assert.False(raster.Boundary[32 * 64 + 40]);
    }

    /// <summary>Without a verified walk, the gap separating adjacent cells remains visible.</summary>
    [Theory]
    [InlineData(PassageKind.Door, true)]
    [InlineData(PassageKind.Breakable, true)]
    [InlineData(PassageKind.Jump, true)]
    [InlineData(PassageKind.Walk, false)]
    public void DoesNotFillUnprovenGaps(PassageKind kind, bool verified)
    {
        var graph = Pair();
        graph.Connect(0, 1, kind, verified);
        var raster = Build(graph);

        Assert.Equal(-1, raster.NodeAt(new Point(1, 0, 0)));
    }

    /// <summary>Samples on a different architectural floor never enter the requested raster.</summary>
    [Fact]
    public void SeparatesStackedFloors()
    {
        var graph = Pair();
        graph.Nodes[1].Floor = 1;
        graph.Connect(0, 1);
        var raster = Build(graph);

        Assert.Equal(-1, raster.NodeAt(graph.Nodes[1].Position));
        Assert.Single(raster.Features);
    }

    /// <summary>A diagonal connection alone cannot fill a square over unsampled corners.</summary>
    [Fact]
    public void PreservesUnsampledCorners()
    {
        var graph = Pair();
        graph.Nodes[1].Position.z = 2;
        graph.Connect(0, 1);
        var raster = Build(graph);

        Assert.Equal(-1, raster.NodeAt(new Point(1, 0, 1)));
    }

    /// <summary>Rasterization does not invent floor across the center of a hollow room.</summary>
    [Fact]
    public void PreservesCourtyardHole()
    {
        var graph = new NavGraph();
        TestSurfaces.Add(graph, 0, 8, 8, 0, true);
        var raster = new FloorPlanRaster(96, 0, new Point(4, 0, 4), 16);
        new FloorPlanBuilder().Build(graph, 1.25f, raster).ToList();

        Assert.Equal(-1, raster.NodeAt(new Point(4, 0, 4)));
        Assert.Contains(true, raster.Boundary);
    }

    /// <summary>A cancelled replacement cannot publish partially rasterized coverage over the previous floor.</summary>
    [Fact]
    public void CacheRetainsCompleteFloorDuringReplacement()
    {
        var graph = Pair();
        var cache = new FloorPlanCache();
        Complete(cache, graph, 0);
        var previous = cache.Ready;
        var budget = new FrameWorkBudget(1, 1000);
        cache.Tick(graph, 1, 2, 64, new Point(0, 0, 0), 8, budget);
        Assert.Same(previous, cache.Ready);

        Complete(cache, graph, 0);
        Assert.Equal(0, cache.Ready.Floor);
        Assert.NotNull(cache.Ready.Graph);
        cache.Dispose();
        Assert.Null(cache.Ready);
    }

    /// <summary>A new scan invalidates coverage even if the selected floor number is unchanged.</summary>
    [Fact]
    public void RebuildsForNewScan()
    {
        var cache = new FloorPlanCache();
        var first = Pair();
        Complete(cache, first, 0);
        var second = Pair();
        second.Nodes[1].Floor = 1;
        Complete(cache, second, 0);

        Assert.Same(second, cache.Ready.Graph);
        Assert.Equal(-1, cache.Ready.NodeAt(second.Nodes[1].Position));
        cache.Dispose();
    }

    /// <summary>Changing projection invalidates otherwise identical graph coverage.</summary>
    [Fact]
    public void RebuildsWhenProjectionChanges()
    {
        var graph = Pair();
        using var cache = new FloorPlanCache();
        Complete(cache, graph, 0);
        cache.Tick(graph, 0, 2, 32, new Point(2, 0, 0), 12, new FrameWorkBudget(100000, 1000));

        Assert.Equal(32, cache.Ready.Size);
        Assert.Equal(12, cache.Ready.Span);
        Assert.Equal(2, cache.Ready.Center.x);
    }

    private static NavGraph Pair()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point(0, 0, 0)));
        graph.Nodes.Add(new NavNode(new Point(2, 0, 0)));
        return graph;
    }

    private static FloorPlanRaster Build(NavGraph graph)
    {
        var raster = new FloorPlanRaster(64, 0, new Point(0, 0, 0), 8);
        new FloorPlanBuilder().Build(graph, 2, raster).ToList();
        return raster;
    }

    private static void Complete(FloorPlanCache cache, NavGraph graph, int floor)
    {
        var budget = new FrameWorkBudget(100000, 1000);
        cache.Tick(graph, floor, 2, 64, new Point(0, 0, 0), 8, budget);
    }
}
