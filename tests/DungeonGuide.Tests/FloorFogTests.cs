using System;
using System.Linq;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects circular exploration, physical floors, opaque unknown space, and saved fog restoration.</summary>
public sealed class FloorFogTests
{
    /// <summary>Revealing has a twenty-five-metre horizontal radius on the physical floor.</summary>
    [Theory]
    [InlineData(25, 0, 0, true)]
    [InlineData(17, 0, 17, true)]
    [InlineData(22, 0, 22, false)]
    [InlineData(25.01f, 0, 0, false)]
    [InlineData(0, 0.91f, 0, true)]
    public void RestrictsRevealToCircularRange(float x, float y, float z, bool expected)
    {
        var explored = new Exploration();
        var sample = new NavNode(new Point(x, y, z));

        Assert.Equal(expected, ExplorationRevealPolicy.TryReveal(explored, sample, new Point(0, 0, 0), 0,
            (_, _) => true));
        Assert.Equal(expected, explored.Has(sample));
    }

    /// <summary>A smaller default does not override an explicitly configured discovery radius.</summary>
    [Fact]
    public void PreservesExplicitRadiusOverride()
    {
        var explored = new Exploration();
        var sample = new NavNode(new Point(30, 0, 0));

        Assert.True(ExplorationRevealPolicy.TryReveal(explored, sample, new Point(), 0, (_, _) => true, 30));
    }

    /// <summary>A wall blocks exploration even when a same-floor sample is near the player.</summary>
    [Fact]
    public void DoesNotRevealThroughWall()
    {
        var explored = new Exploration();
        var sample = new NavNode(new Point(1, 0, 0));

        Assert.False(ExplorationRevealPolicy.TryReveal(explored, sample, new Point(0, 0, 0), 0,
            (_, _) => false));
        Assert.Empty(explored.Export());
    }

    /// <summary>Nearby stacked floors and invalid physical floors cannot invoke visibility or reveal samples.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void IgnoresOtherPhysicalFloors(int physicalFloor)
    {
        var explored = new Exploration();
        var sample = new NavNode(new Point(0, 0.1f, 0));

        Assert.False(ExplorationRevealPolicy.TryReveal(explored, sample, new Point(0, 0, 0), physicalFloor,
            (_, _) => throw new InvalidOperationException("Wrong-floor visibility query")));
    }

    /// <summary>Soft edges exist only inside known coverage and never reduce fog over an unseen adjacent sample.</summary>
    [Fact]
    public void FeathersOnlyInsideExploredCoverage()
    {
        var raster = Raster();
        var explored = new Exploration();
        explored.Reveal(raster.Graph.Nodes[0]);
        var fog = Build(raster, explored);

        Assert.Equal(0, fog.Opacity[32 * 64 + 32]);
        Assert.True(fog.Visible(new Point(0, 0, 0)));
        Assert.False(fog.Visible(new Point(2, 0, 0)));
        Assert.Equal(255, fog.Opacity[32 * 64 + 48]);
        Assert.Contains(fog.Opacity, alpha => alpha > 0 && alpha < 255);
    }

    /// <summary>Restoring stable visited keys reproduces the mask without adding raster data to saves.</summary>
    [Fact]
    public void RestoresFogFromSavedExploration()
    {
        var raster = Raster();
        var original = new Exploration();
        original.Reveal(raster.Graph.Nodes[0]);
        var saved = original.Export();
        var restored = new Exploration();
        restored.Load(saved);

        Assert.Equal(Build(raster, original).Opacity, Build(raster, restored).Opacity);
        Assert.Equal(saved, restored.Export());
    }

    /// <summary>Building a complete-map mask never turns an unvisited sample into saved exploration.</summary>
    [Fact]
    public void MaskGenerationDoesNotModifyExploration()
    {
        var raster = Raster();
        var explored = new Exploration();
        var revision = explored.Revision;
        var fog = Build(raster, explored);

        Assert.All(fog.Opacity, alpha => Assert.Equal(255, alpha));
        Assert.Empty(explored.Export());
        Assert.Equal(revision, explored.Revision);
    }

    /// <summary>Continuous new visits do not restart an unfinished mask and prevent publication.</summary>
    [Fact]
    public void PublishesWhileExplorationContinuesChanging()
    {
        var raster = Raster();
        var explored = new Exploration();
        using var cache = new FloorFogCache();
        for (var frame = 0; frame < 200 && cache.Ready == null; frame++)
        {
            explored.Reveal(new Point(frame, 0, 100));
            cache.Tick(raster, explored, new FrameWorkBudget(1, 1000));
        }

        Assert.NotNull(cache.Ready);
        Assert.Same(raster, cache.Ready.Raster);
    }

    /// <summary>A partially computed mask cannot become visible before opaque unknown pixels are finalized.</summary>
    [Fact]
    public void DoesNotPublishPartialFog()
    {
        var raster = Raster();
        var explored = new Exploration();
        using var cache = new FloorFogCache();
        cache.Tick(raster, explored, new FrameWorkBudget(1, 1000));
        Assert.Null(cache.Ready);
        cache.Tick(raster, explored, new FrameWorkBudget(100000, 1000));

        Assert.NotNull(cache.Ready);
        Assert.All(cache.Ready.Opacity, alpha => Assert.Equal(255, alpha));
    }

    private static FloorPlanRaster Raster()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point(0, 0, 0)));
        graph.Nodes.Add(new NavNode(new Point(2, 0, 0)));
        var raster = new FloorPlanRaster(64, 0, new Point(0, 0, 0), 8);
        new FloorPlanBuilder().Build(graph, 2, raster).ToList();
        return raster;
    }

    private static FloorFogMask Build(FloorPlanRaster raster, Exploration explored)
    {
        var fog = new FloorFogMask(raster);
        fog.Build(explored).ToList();
        return fog;
    }
}
