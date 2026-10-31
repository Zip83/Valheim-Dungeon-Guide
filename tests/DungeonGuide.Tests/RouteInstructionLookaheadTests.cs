using System;
using System.Linq;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects full-leg distances without hiding turns, floor transitions or required traversal actions.</summary>
public sealed class RouteInstructionLookaheadTests
{
    /// <summary>Collinear sampled tiles form one instruction, with distance counting down as the player moves.</summary>
    [Theory]
    [InlineData(0, 0, 12)]
    [InlineData(1, 4, 8)]
    [InlineData(2, 7, 5)]
    public void MeasuresWholeStraightLeg(int cursor, float playerZ, float expected)
    {
        var graph = Line();
        var leg = RouteInstructionLookahead.Measure(graph, Route(graph), cursor, new Point(0, 0, playerZ));

        Assert.Equal(4, leg.End);
        Assert.Equal(expected, leg.Distance, 3);
    }

    /// <summary>A right-angle turn ends the current instruction at the corner rather than the next sample.</summary>
    [Fact]
    public void StopsAtCorner()
    {
        var graph = Path(new Point(), new Point(0, 0, 3), new Point(0, 0, 6), new Point(3, 0, 6));
        var leg = RouteInstructionLookahead.Measure(graph, Route(graph), 0, new Point());

        Assert.Equal(2, leg.End);
        Assert.Equal(6, leg.Distance);
    }

    /// <summary>Small sampling bends remain one leg while a significant heading change starts a new instruction.</summary>
    [Theory]
    [InlineData(20, 2)]
    [InlineData(30, 1)]
    [InlineData(180, 1)]
    public void RespectsHeadingTolerance(float degrees, int expectedEnd)
    {
        var angle = degrees * Math.PI / 180;
        var graph = Path(new Point(), new Point(0, 0, 3),
            new Point((float)Math.Sin(angle) * 3, 0, 3 + (float)Math.Cos(angle) * 3));

        Assert.Equal(expectedEnd, RouteInstructionLookahead.Measure(graph, Route(graph), 0, new Point()).End);
    }

    /// <summary>Doors, obstacles, jumps and drops ahead remain distinct maneuvers.</summary>
    [Theory]
    [InlineData(PassageKind.Door)]
    [InlineData(PassageKind.Breakable)]
    [InlineData(PassageKind.Jump)]
    [InlineData(PassageKind.Drop)]
    [InlineData(PassageKind.Stairs)]
    public void StopsBeforeDifferentPassage(PassageKind kind)
    {
        var graph = Line();
        graph.Nodes[2].Edges[0].Kind = kind;
        var leg = RouteInstructionLookahead.Measure(graph, Route(graph), 0, new Point());

        Assert.Equal(2, leg.End);
        Assert.Equal(6, leg.Distance);
    }

    /// <summary>An immediate special action cannot be swallowed into a later walking instruction.</summary>
    [Theory]
    [InlineData(PassageKind.Door)]
    [InlineData(PassageKind.Breakable)]
    [InlineData(PassageKind.Jump)]
    [InlineData(PassageKind.Drop)]
    public void KeepsImmediateActionSeparate(PassageKind kind)
    {
        var graph = Line();
        graph.Nodes[0].Edges[0].Kind = kind;

        Assert.Equal(1, RouteInstructionLookahead.Measure(graph, Route(graph), 0, new Point()).End);
    }

    /// <summary>The hook fallback stays separate even when its endpoint geometry appears collinear.</summary>
    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 2)]
    public void StopsAtHook(int hookFrom, int expectedEnd)
    {
        var graph = Line();
        var leg = RouteInstructionLookahead.Measure(graph, Route(graph), 0, new Point(),
            (from, to) => from == hookFrom && to == hookFrom + 1);

        Assert.Equal(expectedEnd, leg.End);
    }

    /// <summary>Missing or unverified edges cannot extend an instruction across an unknown connection.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StopsAtUnverifiedConnection(bool missing)
    {
        var graph = Line();
        if (missing)
        {
            graph.Nodes[2].Edges.Clear();
        }
        else
        {
            graph.Nodes[2].Edges[0].Verified = false;
        }

        Assert.Equal(2, RouteInstructionLookahead.Measure(graph, Route(graph), 0, new Point()).End);
    }

    /// <summary>A physical floor transition is an instruction boundary even along an aligned staircase.</summary>
    [Fact]
    public void StopsOnNextFloor()
    {
        var graph = Path(new Point(), new Point(0, 1, 3), new Point(0, 2, 6), new Point(0, 3, 9));
        foreach (var node in graph.Nodes)
        {
            foreach (var edge in node.Edges)
            {
                edge.Kind = PassageKind.Stairs;
            }
        }

        graph.Nodes[2].Floor = graph.Nodes[3].Floor = 1;
        var leg = RouteInstructionLookahead.Measure(graph, Route(graph), 0, new Point());

        Assert.Equal(2, leg.End);
        Assert.Equal(2 * (float)Math.Sqrt(10), leg.Distance, 3);
    }

    /// <summary>An upward segment cannot include a later descent under the same direction indicator.</summary>
    [Fact]
    public void StopsAtSlopeReversal()
    {
        var graph = Path(new Point(), new Point(0, 1, 3), new Point(0, 0, 6));

        Assert.Equal(1, RouteInstructionLookahead.Measure(graph, Route(graph), 0, new Point()).End);
    }

    /// <summary>Routes use their ordered node indices rather than assuming graph storage matches path order.</summary>
    [Fact]
    public void FollowsRouteOrder()
    {
        var graph = Path(new Point(0, 0, 6), new Point(), new Point(0, 0, 3));
        graph.Nodes[1].Edges.Clear();
        graph.Nodes[1].Edges.Add(new NavEdge(2, PassageKind.Walk, true));
        graph.Nodes[2].Edges.Add(new NavEdge(0, PassageKind.Walk, true));
        var leg = RouteInstructionLookahead.Measure(graph, new[] { 1, 2, 0 }, 0, new Point());

        Assert.Equal(2, leg.End);
        Assert.Equal(6, leg.Distance);
    }

    /// <summary>Arrival at the last sample reports only the remaining approach, without looking past the route.</summary>
    [Fact]
    public void HandlesFinalSample()
    {
        var graph = Line();
        var leg = RouteInstructionLookahead.Measure(graph, Route(graph), 4, new Point(0, 0, 11));

        Assert.Equal(4, leg.End);
        Assert.Equal(1, leg.Distance);
    }

    /// <summary>An empty route has no instruction endpoint.</summary>
    [Fact]
    public void HandlesEmptyRoute()
    {
        Assert.Equal((-1, 0f), RouteInstructionLookahead.Measure(new NavGraph(), Array.Empty<int>(), 0, new Point()));
    }

    private static NavGraph Line() => Path(Enumerable.Range(0, 5).Select(i => new Point(0, 0, i * 3)).ToArray());

    private static int[] Route(NavGraph graph) => Enumerable.Range(0, graph.Nodes.Count).ToArray();

    private static NavGraph Path(params Point[] points)
    {
        var graph = new NavGraph();
        foreach (var point in points)
        {
            graph.Nodes.Add(new NavNode(point));
        }

        for (var i = 0; i + 1 < points.Length; i++)
        {
            graph.Nodes[i].Edges.Add(new NavEdge(i + 1, PassageKind.Walk, true));
        }

        return graph;
    }
}
