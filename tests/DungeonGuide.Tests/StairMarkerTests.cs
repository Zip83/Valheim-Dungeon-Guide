using System.Linq;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects vertical arrows from changes in floor classification and passage verification.</summary>
public sealed class StairMarkerTests
{
    /// <summary>Only verified ordinary vertical connections are shown as stairs.</summary>
    [Theory]
    [InlineData(PassageKind.Stairs, true, 0, true)]
    [InlineData(PassageKind.Stairs, false, 1, false)]
    [InlineData(PassageKind.Walk, true, 1, true)]
    [InlineData(PassageKind.Walk, true, 0, false)]
    [InlineData(PassageKind.Jump, true, 1, false)]
    [InlineData(PassageKind.Drop, true, 1, false)]
    public void SelectsSupportedStairConnections(PassageKind kind, bool verified, int floor, bool expected)
    {
        var from = new NavNode(new Point());
        var to = new NavNode(new Point(1, 0.5f, 0)) { Floor = floor };

        Assert.Equal(expected, StairMarkerPolicy.IsTransition(from, to, new NavEdge(1, kind, verified)));
    }

    /// <summary>A landing between stair segments retains both upward and downward guidance.</summary>
    [Fact]
    public void LandingCanPointBothWaysOnSameFloor()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point()));
        graph.Nodes.Add(new NavNode(new Point(1, 0.5f, 0)));
        graph.Nodes.Add(new NavNode(new Point(-1, -0.5f, 0)));
        graph.Nodes[0].Edges.Add(new NavEdge(1, PassageKind.Stairs, true));
        graph.Nodes[0].Edges.Add(new NavEdge(2, PassageKind.Stairs, true));

        Assert.Equal((true, true), StairMarkerPolicy.Directions(graph, 0));
    }

    /// <summary>The raster indexes same-floor stairs so the marker renderer can actually draw their arrows.</summary>
    [Fact]
    public void RasterIncludesSameFloorStairFeatures()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point()));
        graph.Nodes.Add(new NavNode(new Point(1, 0.5f, 0)));
        graph.Connect(0, 1, PassageKind.Stairs, true);
        var raster = new FloorPlanRaster(64, 0, new Point(), 8);
        new FloorPlanBuilder().Build(graph, 1, raster).ToList();

        Assert.Contains(0, raster.Features);
        Assert.Contains(1, raster.Features);
        Assert.Single(raster.StairGroups);
    }

}
