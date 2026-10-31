using System;
using System.IO;
using System.Linq;
using System.Threading;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects cached layouts, version isolation, malformed-data rejection and traversal proof boundaries.</summary>
public sealed class GeometryCacheTests
{
    /// <summary>Geometry, directed passages, floors, landings and the observed entrance survive an independent reload.</summary>
    [Fact]
    public void RoundTripsGeometryWithoutSharingGraphObjects()
    {
        var original = Example();
        var restored = Read(Encode(original));
        Assert.NotSame(original.Graph, restored.Graph);
        Assert.Equal(original.Graph.Nodes.Select(node => node.Key), restored.Graph.Nodes.Select(node => node.Key));
        Assert.Equal(new[] { 5000f, 5003f }, restored.Layout.Levels);
        Assert.Equal(new[] { 2 }, restored.Layout.LandingNodes);
        Assert.Equal("gate", restored.Graph.Nodes[0].Edges[0].Obstacle);
        Assert.Equal(1, restored.Graph.Nodes[0].Edges[0].To);
        Assert.Empty(restored.Graph.Nodes[2].Edges);
        Assert.Equal(1.25f, restored.Spacing);
        Assert.True(restored.Complete);
        Assert.True(restored.Truncated);
        Assert.Equal(Point.Key(original.Entrance.Value), Point.Key(restored.Entrance.Value));
    }

    /// <summary>Cached special passages require current world and player validation before becoming routes.</summary>
    [Theory]
    [InlineData(PassageKind.Door)]
    [InlineData(PassageKind.Breakable)]
    [InlineData(PassageKind.Jump)]
    [InlineData(PassageKind.Drop)]
    public void SpecialTraversalNeedsFreshProofValidation(PassageKind kind)
    {
        var original = Example();
        original.Graph.Nodes[1].Edges[0].Kind = kind;
        Assert.False(Read(Encode(original)).Graph.Nodes[1].Edges[0].Verified);
        Assert.True(original.Graph.Nodes[1].Edges[0].Verified);
    }

    /// <summary>Normal game-thread edge changes between cooperative steps cannot invalidate the writer's enumerator.</summary>
    [Fact]
    public void OwnsEdgeValuesAcrossCooperativePauses()
    {
        var geometry = Example();
        using var writer = new GeometryCacheWriter();
        using var steps = writer.Encode("identity", geometry).GetEnumerator();
        var firstNodeStep = geometry.Layout.Levels.Count + geometry.Layout.LandingNodes.Count + 1;
        for (var i = 0; i < firstNodeStep; i++)
        {
            Assert.True(steps.MoveNext());
        }

        geometry.Graph.Nodes[0].Edges.Clear();
        while (steps.MoveNext())
        {
        }

        Assert.Single(Read(writer.Payload).Graph.Nodes[0].Edges);
    }

    /// <summary>A cache from another layout or world must not be published under the current identity.</summary>
    [Fact]
    public void RejectsMismatchedIdentity()
    {
        using var stream = new MemoryStream(Encode(Example()));
        Assert.Throws<InvalidDataException>(() => GeometryCacheReader.Read(stream, "another-world"));
    }

    /// <summary>Future cache formats are rejected rather than interpreted as current data.</summary>
    [Fact]
    public void RejectsFutureFormat()
    {
        var bytes = Encode(Example());
        using (var stream = new MemoryStream(bytes))
        using (var reader = new BinaryReader(stream))
        {
            GeometryCacheFormat.ReadText(reader);
            Array.Copy(BitConverter.GetBytes(GeometryCacheFormat.Version + 1), 0, bytes, stream.Position, 4);
        }

        Assert.Throws<NotSupportedException>(() => Read(bytes));
    }

    /// <summary>Corrupt coordinates, floor assignments and edge targets never become published geometry.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void RejectsInvalidGeometry(int corruption)
    {
        var geometry = Example();
        switch (corruption)
        {
            case 0:
                geometry.Graph.Nodes[0].Position = new Point(float.NaN, 0, 0);
                break;
            case 1:
                geometry.Graph.Nodes[0].Floor = 9;
                break;
            case 2:
                geometry.Graph.Nodes[0].Edges[0].To = 100;
                break;
            case 3:
                geometry.Layout.Levels[1] = 4999;
                break;
            case 4:
                geometry.Graph.Nodes[0].Edges[0].To = 0;
                break;
        }

        Assert.Throws<InvalidDataException>(() => Read(Encode(geometry)));
    }

    /// <summary>Interrupted bytes and abandoned load requests cannot return a partial map.</summary>
    [Fact]
    public void RejectsTruncationAndHonorsCancellation()
    {
        var bytes = Encode(Example());
        Assert.Throws<EndOfStreamException>(() => Read(bytes.Take(bytes.Length - 1).ToArray()));
        using var stream = new MemoryStream(bytes);
        Assert.Throws<OperationCanceledException>(() => GeometryCacheReader.Read(stream, "identity", new CancellationToken(true)));
    }

    /// <summary>Permission refreshes reuse a completed special index while a replaced graph gets its own records.</summary>
    [Fact]
    public void SpecialPassagesAreIndexedOncePerGraph()
    {
        var geometry = Example();
        geometry.Graph.Nodes[1].Edges[0].Kind = PassageKind.Breakable;
        geometry.Graph.Nodes[1].Edges[0].Obstacle = "barrier";
        var index = new SpecialPassageIndex();
        Assert.True(index.Build(geometry.Graph).Any());
        foreach (var step in index.Build(geometry.Graph))
        {
        }

        Assert.Equal(2, index.Passages.Count);
        Assert.Equal(new[] { "barrier" }, index.BreakablePrefabs);
        Assert.Empty(index.Build(geometry.Graph));
        foreach (var step in index.Build(new NavGraph()))
        {
        }

        Assert.Empty(index.Passages);
        Assert.Empty(index.BreakablePrefabs);
    }

    private static CachedGeometry Example()
    {
        var geometry = new CachedGeometry
        {
            Spacing = 1.25f,
            Complete = true,
            Truncated = true,
            Entrance = new Point(1, 5000, 2)
        };
        geometry.Layout.Levels.AddRange(new[] { 5000f, 5003f });
        geometry.Layout.LandingNodes.Add(2);
        geometry.Graph.Nodes.Add(new NavNode(new Point(0, 5000, 0)) { Floor = 0 });
        geometry.Graph.Nodes.Add(new NavNode(new Point(1, 5000, 0), false) { Floor = 0 });
        geometry.Graph.Nodes.Add(new NavNode(new Point(2, 5003, 0)) { Floor = 1 });
        geometry.Graph.Nodes[0].Edges.Add(new NavEdge(1, PassageKind.Door, true, "gate"));
        geometry.Graph.Nodes[1].Edges.Add(new NavEdge(2, PassageKind.Jump, true));
        return geometry;
    }

    private static byte[] Encode(CachedGeometry geometry)
    {
        using var writer = new GeometryCacheWriter();
        foreach (var step in writer.Encode("identity", geometry))
        {
        }

        return writer.Payload;
    }

    private static CachedGeometry Read(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return GeometryCacheReader.Read(stream, "identity");
    }
}
