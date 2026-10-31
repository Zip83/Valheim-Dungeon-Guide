using System.Linq;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects sparse stair symbols, disconnected flights, floor isolation and partially explored landings.</summary>
public sealed class StairMarkerGroupingTests
{
    /// <summary>A flight with many rows and parallel lanes produces only its central lower and upper symbols.</summary>
    [Theory]
    [InlineData(1, 2)]
    [InlineData(3, 12)]
    [InlineData(9, 60)]
    public void OneFlightSelectsTwoLandingRepresentatives(int width, int steps)
    {
        var graph = new NavGraph();
        AddFlight(graph, width, steps);
        var group = Assert.Single(Build(graph).StairGroups);
        Assert.Equal(width * steps, group.Nodes.Count);
        Assert.Equal((width / 2, (steps - 1) * width + width / 2),
            StairMarkerSelection.Select(graph, group, new Exploration(), true));
    }

    /// <summary>Proximity alone must never merge separate stairs across walls or unsupported actions.</summary>
    [Theory]
    [InlineData(PassageKind.Walk, false)]
    [InlineData(PassageKind.Jump, true)]
    [InlineData(PassageKind.Drop, true)]
    [InlineData(PassageKind.Door, true)]
    [InlineData(PassageKind.Breakable, true)]
    public void KeepsSeparateFlightsAcrossNonOrdinaryConnections(PassageKind kind, bool verified)
    {
        var graph = new NavGraph();
        AddFlight(graph, 1, 4);
        var second = AddFlight(graph, 1, 4, 0.25f);
        graph.Connect(0, second, kind, verified);
        Assert.Equal(2, Build(graph).StairGroups.Count);
    }

    /// <summary>Ordinary floor between two stair regions does not combine them into one large floor-wide cluster.</summary>
    [Fact]
    public void FlatFloorSeparatesStairRegions()
    {
        var graph = new NavGraph();
        AddFlight(graph, 1, 4);
        var second = AddFlight(graph, 1, 4, 3);
        var flat = graph.Nodes.Count;
        graph.Nodes.Add(new NavNode(new Point(1.5f, 0, 0)));
        graph.Connect(0, flat);
        graph.Connect(flat, second);
        Assert.Equal(2, Build(graph).StairGroups.Count);
    }

    /// <summary>A genuine staircase crossing a floor boundary keeps one independently selectable region per floor.</summary>
    [Fact]
    public void StackedFloorsKeepIndependentGroups()
    {
        var graph = new NavGraph();
        AddFlight(graph, 1, 4);
        var upper = AddFlight(graph, 1, 4, floor: 1);
        graph.Connect(3, upper, PassageKind.Stairs);
        Assert.All(Assert.Single(Build(graph).StairGroups).Nodes, id => Assert.Equal(0, graph.Nodes[id].Floor));
        Assert.All(Assert.Single(Build(graph, 1).StairGroups).Nodes, id => Assert.Equal(1, graph.Nodes[id].Floor));
    }

    /// <summary>Grouping geometry does not reveal an unknown flight in explored mode.</summary>
    [Fact]
    public void UnknownFlightHasNoVisibleSymbols()
    {
        var graph = new NavGraph();
        AddFlight(graph, 3, 8);
        var group = Assert.Single(Build(graph).StairGroups);
        Assert.Equal((-1, -1), StairMarkerSelection.Select(graph, group, new Exploration(), false));
    }

    /// <summary>Visible stair samples remain useful before either geometric end has been explored.</summary>
    [Fact]
    public void PartiallyExploredFlightSelectsOnlyKnownSamples()
    {
        var graph = new NavGraph();
        AddFlight(graph, 3, 8);
        var group = Assert.Single(Build(graph).StairGroups);
        var exploration = new Exploration();
        exploration.Reveal(graph.Nodes[10]);
        Assert.Equal((10, 10), StairMarkerSelection.Select(graph, group, exploration, false));
        exploration.Reveal(graph.Nodes[1]);
        exploration.Reveal(graph.Nodes[22]);
        Assert.Equal((1, 22), StairMarkerSelection.Select(graph, group, exploration, false));
    }

    /// <summary>A cached group cannot keep arrows whose actual passage verification has since been revoked.</summary>
    [Fact]
    public void SelectionRechecksCurrentVerification()
    {
        var graph = new NavGraph();
        AddFlight(graph, 1, 8);
        var group = Assert.Single(Build(graph).StairGroups);
        foreach (var edge in graph.Nodes.SelectMany(node => node.Edges))
        {
            edge.Verified = false;
        }

        Assert.Equal((-1, -1), StairMarkerSelection.Select(graph, group, new Exploration(), true));
    }

    /// <summary>Undirected display grouping does not invent reverse travel for directed native connections.</summary>
    [Fact]
    public void OneWayStairsDoNotAcquireDownwardSymbols()
    {
        var graph = new NavGraph();
        for (var i = 0; i < 4; i++)
        {
            graph.Nodes.Add(new NavNode(new Point(0, i * 0.5f, i)));
            if (i > 0)
            {
                graph.Connect(i - 1, i, PassageKind.Stairs, true, false);
            }
        }

        var group = Assert.Single(Build(graph).StairGroups);
        Assert.Equal((0, -1), StairMarkerSelection.Select(graph, group, new Exploration(), true));
        Assert.Empty(graph.Nodes[3].Edges);
    }

    /// <summary>Abandoning a cooperative build cannot alter navigation or publish an unfinished stair region.</summary>
    [Fact]
    public void GroupingYieldsAndCancellationLeavesNavigationUnchanged()
    {
        var graph = new NavGraph();
        AddFlight(graph, 9, 60);
        var raster = new FloorPlanRaster(16, 0, new Point(), 100);
        raster.Features.AddRange(Enumerable.Range(0, graph.Nodes.Count));
        var edges = graph.Nodes.Select(node => node.Edges.Count).ToArray();
        using (var job = new StairMarkerGrouping(graph, raster).Build().GetEnumerator())
        {
            for (var i = 0; i < graph.Nodes.Count + 10; i++)
            {
                Assert.True(job.MoveNext());
            }
        }

        Assert.Empty(raster.StairGroups);
        Assert.Equal(edges, graph.Nodes.Select(node => node.Edges.Count));
        Assert.True(new StairMarkerGrouping(graph, raster).Build().Count() > graph.Nodes.Count * 2);
        Assert.Single(raster.StairGroups);
    }

    private static int AddFlight(NavGraph graph, int width, int steps, float offset = 0, int floor = 0)
    {
        var start = graph.Nodes.Count;
        for (var row = 0; row < steps; row++)
        {
            for (var lane = 0; lane < width; lane++)
            {
                var id = graph.Nodes.Count;
                graph.Nodes.Add(new NavNode(new Point(lane + offset, row * 0.5f + floor * 5, row)) { Floor = floor });
                if (lane > 0)
                {
                    graph.Connect(id - 1, id);
                }

                if (row > 0)
                {
                    graph.Connect(id - width, id, PassageKind.Stairs);
                }
            }
        }

        return start;
    }

    private static FloorPlanRaster Build(NavGraph graph, int floor = 0)
    {
        var raster = new FloorPlanRaster(16, floor, new Point(), 100);
        foreach (var step in new FloorPlanBuilder().Build(graph, 1, raster))
        {
        }

        return raster;
    }
}
