using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DungeonGuide;

/// <summary>Copies published graph values into an exclusively owned binary payload cooperatively.</summary>
internal sealed class GeometryCacheWriter : IDisposable
{
    private readonly MemoryStream _stream = new();
    private readonly BinaryWriter _writer;
    private int _edges;

    /// <summary>Allocates private output storage; graph access stays on the caller's game thread.</summary>
    public GeometryCacheWriter()
    {
        _writer = new BinaryWriter(_stream);
    }

    /// <summary>Returns a sealed payload only after the caller has completed encoding.</summary>
    public byte[] Payload => _stream.ToArray();

    /// <summary>Current buffered byte count for enforcing the capture memory limit between work steps.</summary>
    public long Length => _stream.Length;

    /// <summary>Writes one owned value per work step, including independent copies of mutable edge lists.</summary>
    public IEnumerable<byte> Encode(string key, CachedGeometry geometry)
    {
        WriteHeader(key, geometry);
        foreach (var step in WriteLevels(geometry))
        {
            yield return step;
        }

        foreach (var step in WriteLandings(geometry))
        {
            yield return step;
        }

        foreach (var step in WriteGraph(geometry))
        {
            yield return step;
        }

        _writer.Flush();
    }

    private IEnumerable<byte> WriteLevels(CachedGeometry geometry)
    {
        foreach (var level in geometry.Layout.Levels)
        {
            _writer.Write(level);
            yield return 0;
        }
    }

    private IEnumerable<byte> WriteLandings(CachedGeometry geometry)
    {
        _writer.Write(geometry.Layout.LandingNodes.Count);
        foreach (var node in geometry.Layout.LandingNodes)
        {
            _writer.Write(node);
            yield return 0;
        }
    }

    private IEnumerable<byte> WriteGraph(CachedGeometry geometry)
    {
        foreach (var node in geometry.Graph.Nodes)
        {
            foreach (var step in WriteNode(node))
            {
                yield return step;
            }
        }
    }

    private void WriteHeader(string key, CachedGeometry geometry)
    {
        GeometryCacheFormat.WriteText(_writer, "ValheimDungeonGeometry");
        _writer.Write(GeometryCacheFormat.Version);
        GeometryCacheFormat.WriteText(_writer, key);
        _writer.Write(geometry.Spacing);
        _writer.Write(geometry.Complete);
        _writer.Write(geometry.Truncated);
        _writer.Write(geometry.Entrance.HasValue);
        if (geometry.Entrance.HasValue)
        {
            GeometryCacheFormat.WritePoint(_writer, geometry.Entrance.Value);
        }

        _writer.Write(GeometryCacheFormat.Count(geometry.Graph.Nodes.Count, GeometryCacheFormat.MaxNodes));
        _writer.Write(GeometryCacheFormat.Count(geometry.Layout.Levels.Count, GeometryCacheFormat.MaxFloors));
    }

    private IEnumerable<byte> WriteNode(NavNode node)
    {
        GeometryCacheFormat.WritePoint(_writer, node.Position);
        _writer.Write(node.Flat);
        _writer.Write(node.Floor);
        var edges = node.Edges.Select(edge => new NavEdge(edge.To, edge.Kind, edge.Verified, edge.Obstacle)).ToArray();
        _edges = GeometryCacheFormat.Count(_edges + edges.Length, GeometryCacheFormat.MaxEdges);
        _writer.Write(edges.Length);
        yield return 0;
        foreach (var edge in edges)
        {
            _writer.Write(edge.To);
            _writer.Write((byte)edge.Kind);
            _writer.Write(edge.Verified && (edge.Kind == PassageKind.Walk || edge.Kind == PassageKind.Stairs));
            GeometryCacheFormat.WriteText(_writer, edge.Obstacle);
            yield return 0;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _writer.Dispose();
    }
}
