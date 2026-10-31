using System;
using System.Linq;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects circular exploration, physical floors, opaque unknown space, and saved fog restoration.</summary>
public sealed class FloorFogTests
{
    /// <summary>Revealing has a ten-metre horizontal radius on the physical floor.</summary>
    [Theory]
    [InlineData(10, 0, 0, true)]
    [InlineData(-10, 0, 0, true)]
    [InlineData(0, 0, 10, true)]
    [InlineData(0, 0, -10, true)]
    [InlineData(6, 0, 8, true)]
    [InlineData(7, 0, 7, true)]
    [InlineData(8, 0, 8, false)]
    [InlineData(-8, 0, -8, false)]
    [InlineData(10, 0, 10, false)]
    [InlineData(10.01f, 0, 0, false)]
    [InlineData(0, 0.91f, 0, true)]
    public void RestrictsRevealToCircularRange(float x, float y, float z, bool expected)
    {
        var explored = new Exploration();
        var sample = new NavNode(new Point(x, y, z));

        Assert.Equal(expected, ExplorationRevealPolicy.TryReveal(explored, sample, new Point(0, 0, 0), 0,
            (_, _) => true));
        Assert.Equal(expected, explored.Has(sample));
    }

    /// <summary>A smaller configured radius includes its boundary and excludes more distant samples.</summary>
    [Theory]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void PreservesSmallerRadiusOverride(float distance, bool expected)
    {
        var explored = new Exploration();
        var sample = new NavNode(new Point(distance, 0, 0));

        Assert.Equal(expected, ExplorationRevealPolicy.TryReveal(explored, sample, new Point(), 0, (_, _) => true, 5));
    }

    /// <summary>Legacy radius values cannot reveal beyond ten metres or invoke distant visibility queries.</summary>
    [Theory]
    [InlineData(25)]
    [InlineData(100)]
    public void CapsLegacyRadius(float radius)
    {
        var explored = new Exploration();
        var sample = new NavNode(new Point(11, 0, 0));

        Assert.False(ExplorationRevealPolicy.TryReveal(explored, sample, new Point(), 0,
            (_, _) => throw new InvalidOperationException("Out-of-range visibility query"), radius));
        Assert.Empty(explored.Export());
    }

    /// <summary>Moving the player translates the circular boundary away from the world origin.</summary>
    [Fact]
    public void CentersCircleOnPlayer()
    {
        var explored = new Exploration();
        var player = new Point(100, 0, -50);
        var boundary = new NavNode(new Point(106, 0, -42));
        var corner = new NavNode(new Point(108, 0, -42));

        Assert.True(ExplorationRevealPolicy.TryReveal(explored, boundary, player, 0, (_, _) => true));
        Assert.False(ExplorationRevealPolicy.TryReveal(explored, corner, player, 0,
            (_, _) => throw new InvalidOperationException("Square-corner visibility query")));
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
