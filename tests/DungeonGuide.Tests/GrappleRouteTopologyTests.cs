using System.Linq;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects directed hook fallback segments without inventing ordinary or reverse passages.</summary>
public sealed class GrappleRouteTopologyTests
{
    /// <summary>One hook joins a reachable launch to a landing with an ordinary directed route to the goal.</summary>
    [Fact]
    public void JoinsApproachAndDeparture()
    {
        var graph = Disconnected();
        var topology = Build(graph);
        Assert.Equal(new[] { 0, 1, 2, 3 }, topology.Join(1, 2));
        Assert.Equal(new[] { 0, 1 }, topology.Sources.OrderBy(n => n));
        Assert.Equal(2, graph.Nodes.Sum(n => n.Edges.Count));
    }

    /// <summary>A forward-only drop cannot be traversed backward to create a departure route.</summary>
    [Fact]
    public void DoesNotReverseDepartureEdges()
    {
        var graph = Disconnected();
        graph.Nodes[2].Edges.Clear();
        graph.Connect(3, 2, PassageKind.Drop, true, false);
        Assert.False(Build(graph).IsLanding(2));
    }

    /// <summary>Unobserved jumps cannot make the launch reachable, while verified jumps need no opt-in.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RequiresVerifiedApproach(bool verified)
    {
        var graph = Disconnected();
        graph.Nodes[0].Edges.Clear();
        graph.Connect(0, 1, PassageKind.Jump, verified, false);
        Assert.Equal(verified, Build(graph).Sources.Contains(1));
    }

    /// <summary>A connected ordinary route leaves no target-reaching component eligible for a hook shortcut.</summary>
    [Fact]
    public void DoesNotOfferHookWhenOrdinaryRouteExists()
    {
        var graph = Disconnected();
        graph.Connect(1, 2);
        var topology = Build(graph);
        Assert.All(Enumerable.Range(0, 4), n => Assert.False(topology.IsLanding(n)));
        Assert.False(topology.HasLandings);
        Assert.Empty(topology.Join(1, 2));
    }

    /// <summary>An unmatched or missing destination terminates before any landing physics is scheduled.</summary>
    [Fact]
    public void MissingGoalHasNoLandingSearch()
    {
        var topology = new GrappleRouteTopology();
        foreach (var pause in topology.Build(Disconnected(), 0, n => false, new RoutePolicy(),
            new FrameWorkBudget(100, 1, () => 0)))
        {
        }

        Assert.False(topology.HasLandings);
        Assert.Empty(topology.Join(1, 2));
    }

    /// <summary>Destructive approach verification applies equally to ordinary and fallback routing.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RespectsBreakableVerification(bool verified)
    {
        var graph = Disconnected();
        graph.Nodes[0].Edges.Clear();
        graph.Connect(0, 1, PassageKind.Breakable, verified, false);
        Assert.Equal(verified, Build(graph).Sources.Contains(1));
    }

    /// <summary>Small work slices yield repeatedly, including graphs with many disconnected nodes.</summary>
    [Fact]
    public void BuildsAcrossBudgetedSlices()
    {
        var graph = Disconnected();
        var topology = new GrappleRouteTopology();
        var pauses = topology.Build(graph, 0, n => n == 3, new RoutePolicy(),
            new FrameWorkBudget(1, 1, () => 0)).Count();
        Assert.True(pauses >= 4);
        Assert.Equal(new[] { 0, 1, 2, 3 }, topology.Join(1, 2));
    }

    /// <summary>Disposing a suspended search never adds candidate edges to the authoritative graph.</summary>
    [Fact]
    public void CancellationLeavesGraphUnchanged()
    {
        var graph = Disconnected();
        var job = new GrappleRouteTopology().Build(graph, 0, n => n == 3, new RoutePolicy(),
            new FrameWorkBudget(1, 1, () => 0)).GetEnumerator();
        Assert.True(job.MoveNext());
        job.Dispose();
        Assert.Equal(2, graph.Nodes.Sum(n => n.Edges.Count));
        Assert.Empty(graph.Nodes[1].Edges);
    }

    /// <summary>A landing or launch missing from the checked topology cannot produce a partial route.</summary>
    [Theory]
    [InlineData(9, 2)]
    [InlineData(1, 9)]
    [InlineData(2, 3)]
    public void RejectsUnknownEndpoints(int source, int destination)
    {
        Assert.Empty(Build(Disconnected()).Join(source, destination));
    }

    private static GrappleRouteTopology Build(NavGraph graph)
    {
        var topology = new GrappleRouteTopology();
        foreach (var pause in topology.Build(graph, 0, n => n == 3, new RoutePolicy(),
            new FrameWorkBudget(100, 1, () => 0)))
        {
        }

        return topology;
    }

    private static NavGraph Disconnected()
    {
        var graph = new NavGraph();
        foreach (var x in Enumerable.Range(0, 4))
        {
            graph.Nodes.Add(new NavNode(new Point(x, 0, 0)));
        }

        graph.Connect(0, 1, PassageKind.Walk, true, false);
        graph.Connect(2, 3, PassageKind.Walk, true, false);
        return graph;
    }
}
