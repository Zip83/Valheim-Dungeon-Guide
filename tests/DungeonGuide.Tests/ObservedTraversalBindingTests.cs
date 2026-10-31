using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects observed jump links outside the automatic candidate envelope and their directionality.</summary>
public sealed class ObservedTraversalBindingTests
{
    /// <summary>Actual evidence can bind longer, diagonal and rising jumps absent from automatic candidates.</summary>
    [Theory]
    [InlineData(4, 0, 0, PassageKind.Jump)]
    [InlineData(3, 0, 3, PassageKind.Jump)]
    [InlineData(4, 2, 0, PassageKind.Jump)]
    [InlineData(4, -1, 0, PassageKind.Drop)]
    public void AddsMissingDirectedEdge(float x, float y, float z, PassageKind kind)
    {
        var graph = Graph(new Point(x, y, z));
        var index = new Dictionary<(string, string), NavEdge>();
        var proof = new TraversalProof { jumpForce = 6, jumpSkill = 0.5f };

        Assert.True(ObservedTraversalBinding.Bind(graph, index, proof, 0, 1));
        var edge = Assert.Single(graph.Nodes[0].Edges);
        Assert.Equal(kind, edge.Kind);
        Assert.False(edge.Verified);
        Assert.Empty(graph.Nodes[1].Edges);
        Assert.Equal(graph.Nodes[1].Key, Point.Key(proof.to));
        new TraversalProofVerifier().Apply(index, new[] { proof }, 6, 0.5f, 10, 10);
        Assert.True(edge.Verified);
        Assert.Equal(new[] { 0, 1 }, new RoutePlanner().Find(graph, 0, n => n == 1, new RoutePolicy()));
        Assert.Empty(new RoutePlanner().Find(graph, 1, n => n == 0, new RoutePolicy()));
    }

    /// <summary>Successful movement cannot replace an existing door or ordinary floor connection.</summary>
    [Theory]
    [InlineData(PassageKind.Walk)]
    [InlineData(PassageKind.Stairs)]
    [InlineData(PassageKind.Door)]
    [InlineData(PassageKind.Breakable)]
    public void PreservesExistingPassage(PassageKind kind)
    {
        var graph = Graph(new Point(4, 0, 0));
        graph.Connect(0, 1, kind, false, false);
        var index = new Dictionary<(string, string), NavEdge>();

        Assert.False(ObservedTraversalBinding.Bind(graph, index, new TraversalProof(), 0, 1));
        Assert.Equal(kind, Assert.Single(graph.Nodes[0].Edges).Kind);
        Assert.False(graph.Nodes[0].Edges[0].Verified);
        Assert.Empty(index);
    }

    /// <summary>Rebinding an existing candidate preserves one edge and indexes its current instance.</summary>
    [Fact]
    public void ReusesExistingCandidate()
    {
        var graph = Graph(new Point(2, 0, 0));
        graph.Connect(0, 1, PassageKind.Jump, true, false);
        var index = new Dictionary<(string, string), NavEdge>();
        var proof = new TraversalProof();

        Assert.True(ObservedTraversalBinding.Bind(graph, index, proof, 0, 1));
        Assert.False(graph.Nodes[0].Edges[0].Verified);
        Assert.True(ObservedTraversalBinding.Bind(graph, index, proof, 0, 1));
        Assert.Same(Assert.Single(graph.Nodes[0].Edges), Assert.Single(index).Value);
    }

    /// <summary>Unavailable endpoints and same-node hops cannot create traversal edges.</summary>
    [Theory]
    [InlineData(-1, 1)]
    [InlineData(0, -1)]
    [InlineData(0, 2)]
    [InlineData(0, 0)]
    public void RejectsInvalidEndpoints(int from, int to)
    {
        var graph = Graph(new Point(4, 0, 0));

        Assert.False(ObservedTraversalBinding.Bind(graph, new Dictionary<(string, string), NavEdge>(),
            new TraversalProof(), from, to));
        Assert.All(graph.Nodes, node => Assert.Empty(node.Edges));
    }

    private static NavGraph Graph(Point destination)
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point()));
        graph.Nodes.Add(new NavNode(destination));
        return graph;
    }
}
