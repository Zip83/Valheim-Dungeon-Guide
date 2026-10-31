using System.Linq;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects goal-directed edge searches from interior-cell scans and disconnected false landings.</summary>
public sealed class GrappleCandidateIndexTests
{
    /// <summary>The closest reachable edge to the exit is checked before the player's starting sample.</summary>
    [Fact]
    public void PrioritizesLaunchNearGoal()
    {
        var graph = Corridor();
        var index = Build(graph, 2, 40);
        Assert.Equal(new[] { 1, 0 }, index.Sources);
        Assert.Equal(new[] { 2 }, index.Columns.Values.SelectMany(nodes => nodes));
    }

    /// <summary>Whole reachable regions farther away than the hook range do not become physics candidates.</summary>
    [Fact]
    public void PrunesDistantLaunches()
    {
        var graph = Corridor();
        var index = Build(graph, 2, 3);
        Assert.Equal(new[] { 1 }, index.Sources);
    }

    /// <summary>Interior samples with four permitted neighboring steps are excluded from both sides of a crossing.</summary>
    [Fact]
    public void ExcludesRoomInteriorsOnBothSides()
    {
        var graph = new NavGraph();
        AddRoom(graph, 0);
        AddRoom(graph, 8);
        var index = Build(graph, 13, 40);
        Assert.DoesNotContain(4, index.Sources);
        Assert.DoesNotContain(13, index.Columns.Values.SelectMany(nodes => nodes));
        Assert.Equal(8, index.Sources.Count());
        Assert.Equal(8, index.Columns.Values.Sum(nodes => nodes.Count));
    }

    /// <summary>A one-way or forbidden passage cannot conceal a real boundary behind an inaccessible neighbor.</summary>
    [Theory]
    [InlineData(PassageKind.Jump, true)]
    [InlineData(PassageKind.Walk, false)]
    public void MissingOrdinaryStepRemainsBoundary(PassageKind kind, bool member)
    {
        var graph = new NavGraph();
        AddRoom(graph, 0);
        var edge = graph.Nodes[4].Edges[0];
        edge.Kind = kind;
        Assert.True(GrappleLaunchBoundary.Contains(graph, 4, 1, new RoutePolicy(), node => member));
    }

    /// <summary>Large components yield while indexing and preserve the published graph's directed edges.</summary>
    [Fact]
    public void IndexesCooperativelyWithoutGraphChanges()
    {
        var graph = new NavGraph();
        for (var i = 0; i < 20001; i++)
        {
            graph.Nodes.Add(new NavNode(new Point(i, 0, 0)));
            if (i > 0 && i < 20000)
            {
                graph.Connect(i - 1, i);
            }
        }

        var topology = new GrappleRouteTopology();
        var work = new FrameWorkBudget(64, 1000, () => 0);
        foreach (var pause in topology.Build(graph, 0, node => node == 20000, new RoutePolicy(), work))
        {
        }

        var index = new GrappleCandidateIndex();
        var pauses = index.Build(graph, topology, 1, 40, new RoutePolicy(), work).Count();
        Assert.True(pauses > 300);
        Assert.Equal(19999, index.Sources.First());
        Assert.InRange(index.Sources.Count(), 1, 41);
        Assert.Equal(39998, graph.Nodes.Sum(node => node.Edges.Count));
    }

    /// <summary>Two large rooms use their perimeter candidates instead of a quadratic all-tile crossing search.</summary>
    [Fact]
    public void LargeRoomsSearchOnlyReachablePerimeters()
    {
        var graph = new NavGraph();
        AddRoom(graph, 0, 100);
        AddRoom(graph, 108, 100);
        var index = Build(graph, 15050, 40);
        Assert.Equal(20000, graph.Nodes.Count);
        Assert.InRange(index.Sources.Count(), 1, 200);
        Assert.Equal(396, index.Columns.Values.Sum(nodes => nodes.Count));
    }

    private static NavGraph Corridor()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point()));
        graph.Nodes.Add(new NavNode(new Point(8, 0, 0)));
        graph.Nodes.Add(new NavNode(new Point(10, 0, 0)));
        graph.Connect(0, 1);
        return graph;
    }

    private static GrappleCandidateIndex Build(NavGraph graph, int goal, float range)
    {
        var work = new FrameWorkBudget(64, 1000, () => 0);
        var policy = new RoutePolicy();
        var topology = new GrappleRouteTopology();
        foreach (var pause in topology.Build(graph, 0, node => node == goal, policy, work))
        {
        }

        var index = new GrappleCandidateIndex();
        foreach (var pause in index.Build(graph, topology, 1, range, policy, work))
        {
        }

        return index;
    }

    private static void AddRoom(NavGraph graph, int offset, int side = 3)
    {
        for (var y = 0; y < side; y++)
        {
            for (var x = 0; x < side; x++)
            {
                var node = graph.Nodes.Count;
                graph.Nodes.Add(new NavNode(new Point(offset + x, 0, y)));
                if (x > 0)
                {
                    graph.Connect(node - 1, node);
                }

                if (y > 0)
                {
                    graph.Connect(node - side, node);
                }
            }
        }
    }
}
