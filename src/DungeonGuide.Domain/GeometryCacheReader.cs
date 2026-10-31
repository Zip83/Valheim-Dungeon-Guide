using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DungeonGuide;

/// <summary>Reconstructs a privately owned, validated graph from a matching bounded cache payload.</summary>
internal static class GeometryCacheReader
{
    /// <summary>Rejects unsupported, mismatched, truncated or invalid geometry before it can be published.</summary>
    public static CachedGeometry Read(Stream stream, string key, CancellationToken cancellation = default)
    {
        using var reader = new BinaryReader(stream);
        var magic = GeometryCacheFormat.ReadText(reader);
        var version = reader.ReadInt32();
        if (magic != "ValheimDungeonGeometry" || version < GeometryCacheFormat.Version)
        {
            throw new InvalidDataException("Unsupported dungeon geometry cache format.");
        }

        if (version > GeometryCacheFormat.Version)
        {
            throw new NotSupportedException("The geometry cache requires a newer plugin; preserve the file.");
        }

        if (GeometryCacheFormat.ReadText(reader) != key)
        {
            throw new InvalidDataException("Dungeon geometry cache identity does not match.");
        }

        var geometry = ReadHeader(reader);
        var count = GeometryCacheFormat.Count(reader.ReadInt32(), GeometryCacheFormat.MaxNodes);
        ReadLayout(reader, geometry.Layout, count);
        ReadGraph(reader, geometry, count, cancellation);
        if (stream.Position != stream.Length)
        {
            throw new InvalidDataException("Dungeon geometry cache has unexpected trailing data.");
        }

        return geometry;
    }

    private static CachedGeometry ReadHeader(BinaryReader reader)
    {
        var spacing = GeometryCacheFormat.Finite(reader.ReadSingle());
        if (spacing < 0.75f || spacing > 3)
        {
            throw new InvalidDataException("Unsupported cached scan spacing.");
        }

        var geometry = new CachedGeometry
        {
            Spacing = spacing,
            Complete = reader.ReadBoolean(),
            Truncated = reader.ReadBoolean()
        };
        if (reader.ReadBoolean())
        {
            geometry.Entrance = GeometryCacheFormat.ReadPoint(reader);
        }

        return geometry;
    }

    private static void ReadLayout(BinaryReader reader, FloorLayout layout, int nodes)
    {
        var levels = GeometryCacheFormat.Count(reader.ReadInt32(), GeometryCacheFormat.MaxFloors);
        if (levels == 0 && nodes > 0)
        {
            throw new InvalidDataException("Cached nodes have no floor layout.");
        }

        for (var i = 0; i < levels; i++)
        {
            var level = GeometryCacheFormat.Finite(reader.ReadSingle());
            if (i > 0 && level < layout.Levels[i - 1])
            {
                throw new InvalidDataException("Cached floor levels are not ordered by elevation.");
            }

            layout.Levels.Add(level);
        }

        ReadLandings(reader, layout, nodes);
    }

    private static void ReadLandings(BinaryReader reader, FloorLayout layout, int nodes)
    {
        var landings = GeometryCacheFormat.Count(reader.ReadInt32(), nodes);
        for (var i = 0; i < landings; i++)
        {
            if (!layout.LandingNodes.Add(GeometryCacheFormat.Node(reader.ReadInt32(), nodes)))
            {
                throw new InvalidDataException("Duplicate cached floor landing.");
            }
        }
    }

    private static void ReadGraph(BinaryReader reader, CachedGeometry geometry, int count, CancellationToken cancellation)
    {
        var totalEdges = 0;
        for (var i = 0; i < count; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            var node = new NavNode(GeometryCacheFormat.ReadPoint(reader), reader.ReadBoolean())
            {
                Floor = GeometryCacheFormat.Node(reader.ReadInt32(), geometry.Layout.Levels.Count)
            };
            var edges = GeometryCacheFormat.Count(reader.ReadInt32(), GeometryCacheFormat.MaxEdges - totalEdges);
            totalEdges += edges;
            ReadEdges(reader, node, i, edges, count, cancellation);
            geometry.Graph.Nodes.Add(node);
        }
    }

    private static void ReadEdges(BinaryReader reader, NavNode node, int source, int count, int nodes,
        CancellationToken cancellation)
    {
        var seen = new HashSet<int>();
        for (var i = 0; i < count; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            var target = GeometryCacheFormat.Node(reader.ReadInt32(), nodes);
            if (target == source || !seen.Add(target))
            {
                throw new InvalidDataException("Duplicate or self-referencing cached passage.");
            }

            var kind = (PassageKind)reader.ReadByte();
            if (kind < PassageKind.Walk || kind > PassageKind.Drop)
            {
                throw new InvalidDataException("Unknown cached passage kind.");
            }

            var verified = reader.ReadBoolean() && (kind == PassageKind.Walk || kind == PassageKind.Stairs);
            var obstacle = GeometryCacheFormat.ReadText(reader);
            node.Edges.Add(new NavEdge(target, kind, verified, obstacle.Length == 0 ? null : obstacle));
        }
    }
}
