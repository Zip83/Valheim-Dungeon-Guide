using System.Linq;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>
/// Protects graph connectivity, traversal direction, and route safety policies.
/// </summary>
public sealed class RoutePlannerTests
{
    /// <summary>
    /// Connected stairs must be used in both directions between stacked floors.
    /// </summary>
    [Theory]
    [InlineData(0, 4, new[] { 0, 1, 2, 3, 4 })]
    [InlineData(4, 0, new[] { 4, 3, 2, 1, 0 })]
    [InlineData(2, 2, new[] { 2 })]
    public void FindsConnectedRoute(int start, int destination, int[] expected)
    {
        var route = new RoutePlanner().Find(ConnectedFloors(), start, n => n == destination, new RoutePolicy());
        Assert.Equal(expected, route);
    }

    /// <summary>
    /// Invalid starts and disconnected destinations must not produce invented routes.
    /// </summary>
    [Theory]
    [InlineData(-1, 4)]
    [InlineData(0, 6)]
    public void RejectsUnreachableRoute(int start, int destination)
    {
        var route = new RoutePlanner().Find(ConnectedFloors(), start, n => n == destination, new RoutePolicy());
        Assert.Empty(route);
    }

    /// <summary>
    /// Snapping must distinguish points on vertically stacked floors.
    /// </summary>
    [Fact]
    public void SnapsToMatchingHeight()
    {
        Assert.Equal(4, ConnectedFloors().Nearest(new Point(0, 2.9f, 0), 1));
    }

    /// <summary>
    /// Repeated connection requests must not duplicate adjacency entries.
    /// </summary>
    [Fact]
    public void DoesNotDuplicateEdges()
    {
        var graph = ConnectedFloors();
        graph.Connect(0, 1);
        Assert.Equal(2, graph.Nodes[0].Edges.Count);
    }

    /// <summary>
    /// Special passages require both verification and the corresponding opt-in.
    /// </summary>
    [Theory]
    [InlineData(PassageKind.Breakable, true, false, false, false)]
    [InlineData(PassageKind.Breakable, true, true, false, true)]
    [InlineData(PassageKind.Drop, true, false, false, false)]
    [InlineData(PassageKind.Drop, true, false, true, true)]
    [InlineData(PassageKind.Jump, false, false, true, false)]
    [InlineData(PassageKind.Breakable, false, true, false, false)]
    public void AppliesSpecialPassagePolicy(PassageKind kind, bool verified, bool breakables, bool jumps, bool allowed)
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point(0, 0, 0)));
        graph.Nodes.Add(new NavNode(new Point(1, 0, 0)));
        graph.Connect(0, 1, kind, verified, false);
        var policy = new RoutePolicy
        {
            Breakables = breakables,
            Jumps = jumps
        };
        var route = new RoutePlanner().Find(graph, 0, n => n == 1, policy);
        Assert.Equal(allowed, route.Count == 2);
    }

    /// <summary>
    /// A recorded drop must not imply that the player can climb back up.
    /// </summary>
    [Fact]
    public void DropDoesNotCreateReturnEdge()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point(0, 2, 0)));
        graph.Nodes.Add(new NavNode(new Point(1, 0, 0)));
        graph.Connect(0, 1, PassageKind.Drop, true, false);
        Assert.Empty(new RoutePlanner().Find(graph, 1, n => n == 0, new RoutePolicy { Jumps = true }));
    }

    /// <summary>
    /// A longer ordinary route is preferred to an avoidable destructive shortcut.
    /// </summary>
    [Fact]
    public void PrefersOrdinaryRoute()
    {
        var graph = new NavGraph();
        foreach (var point in new[]
        {
            new Point(0, 0, 0),
            new Point(1, 0, 0),
            new Point(0, 0, 2),
            new Point(2, 0, 0)
        }
        )
        {
            graph.Nodes.Add(new NavNode(point));
        }

        graph.Connect(0, 1, PassageKind.Breakable, true);
        graph.Connect(1, 3);
        graph.Connect(0, 2);
        graph.Connect(2, 3);
        Assert.Equal(
            new[] { 0, 2, 3 },
            new RoutePlanner().Find(graph, 0, n => n == 3, new RoutePolicy { Breakables = true }));
    }

    private static NavGraph ConnectedFloors()
    {
        var graph = new NavGraph();
        foreach (var point in new[]
        {
            new Point(0, 0, 0),
            new Point(2, 0, 0),
            new Point(4, 1, 0),
            new Point(4, 3, 2),
            new Point(0, 3, 0),
            new Point(0, 0, 5),
            new Point(10, 6, 10)
        }
        )
        {
            graph.Nodes.Add(new NavNode(point));
        }

        graph.Connect(0, 1);
        graph.Connect(1, 2);
        graph.Connect(2, 3);
        graph.Connect(3, 4);
        graph.Connect(0, 5);
        return graph;
    }
}
