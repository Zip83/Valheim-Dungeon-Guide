using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects explicit placement against hidden geometry, empty space, walls and stacked floors.</summary>
public sealed class WaypointMapPolicyTests
{
    /// <summary>Placement uses the actual sampled physical elevation, not the floor index or player height.</summary>
    [Fact]
    public void UsesKnownPhysicalSample()
    {
        var raster = Raster();
        var exploration = new Exploration();
        exploration.Reveal(raster.Graph.Nodes[0]);
        var result = WaypointMapPolicy.Resolve(raster, 0.5f, 0.5f, exploration, false);

        Assert.Same(raster.Graph.Nodes[0], result);
        Assert.Equal(12, result.Position.y);
        Assert.Single(exploration.Export());
    }

    /// <summary>Full permits intentional placement on known scan geometry without marking it explored.</summary>
    [Fact]
    public void FullPlacementDoesNotRevealGeometry()
    {
        var raster = Raster();
        var exploration = new Exploration();
        Assert.Null(WaypointMapPolicy.Resolve(raster, 0.5f, 0.5f, exploration, false));
        Assert.NotNull(WaypointMapPolicy.Resolve(raster, 0.5f, 0.5f, exploration, true));
        Assert.Empty(exploration.Export());
    }

    /// <summary>Even Full cannot place into a wall, geometric void or a different physical floor's owner.</summary>
    [Theory]
    [InlineData(true, 1, 2)]
    [InlineData(false, 0, 2)]
    [InlineData(false, 1, 1)]
    public void RejectsInvalidFloorCoverage(bool boundary, int owner, int nodeFloor)
    {
        var raster = Raster();
        raster.Boundary[10] = boundary;
        raster.Owners[10] = owner;
        raster.Graph.Nodes[0].Floor = nodeFloor;
        Assert.Null(WaypointMapPolicy.Resolve(raster, 0.5f, 0.5f, new Exploration(), true));
    }

    /// <summary>Off-image and nonfinite pointer coordinates cannot produce an array access or marker.</summary>
    [Theory]
    [InlineData(-0.1f, 0.5f)]
    [InlineData(1f, 0.5f)]
    [InlineData(0.5f, float.NaN)]
    [InlineData(float.PositiveInfinity, 0.5f)]
    public void RejectsOutsidePointer(float u, float v)
    {
        Assert.Null(WaypointMapPolicy.Resolve(Raster(), u, v, new Exploration(), true));
    }

    private static FloorPlanRaster Raster()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point(1, 12, 2)) { Floor = 2 });
        var raster = new FloorPlanRaster(4, 2, new Point(), 8) { Graph = graph };
        raster.Owners[10] = 1;
        return raster;
    }
}
