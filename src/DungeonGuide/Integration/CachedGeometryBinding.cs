using System;
using System.Collections.Generic;
using System.Threading;

namespace DungeonGuide;

/// <summary>Converts retained graph data into session scan indexes without calling Unity APIs.</summary>
internal static class CachedGeometryBinding
{
    /// <summary>Wraps a published snapshot; the cooperative writer copies its values on the game thread.</summary>
    public static CachedGeometry Capture(DungeonSession session) => new(session.Scan.Graph, session.Layout)
    {
        Spacing = session.Scan.Spacing,
        Truncated = session.Scan.Truncated,
        Complete = !session.HasPartialGeometry && !session.GeometryNeedsScan && !session.GeometryDirty,
        Entrance = session.Area.ObservedEntrance ? session.Area.Entrance : null
    };

    /// <summary>Rebuilds spatial and traversal-candidate indexes on a private worker-owned scan.</summary>
    public static ScanResult Index(CachedGeometry geometry, CancellationToken cancellation)
    {
        var scan = new ScanResult(geometry.Graph) { Spacing = geometry.Spacing, Truncated = geometry.Truncated };
        for (var i = 0; i < scan.Graph.Nodes.Count; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            IndexColumn(scan, i);
            IndexTraversals(scan, i);
        }

        foreach (var step in scan.SpecialPassages.Build(scan.Graph, cancellation))
        {
        }

        return scan;
    }

    private static void IndexColumn(ScanResult scan, int index)
    {
        var node = scan.Graph.Nodes[index];
        var column = ((int)Math.Round(node.Position.x / scan.Spacing), (int)Math.Round(node.Position.z / scan.Spacing));
        if (!scan.Columns.TryGetValue(column, out var nodes))
        {
            nodes = new List<int>();
            scan.Columns.Add(column, nodes);
        }

        nodes.Add(index);
    }

    private static void IndexTraversals(ScanResult scan, int index)
    {
        var node = scan.Graph.Nodes[index];
        foreach (var edge in node.Edges)
        {
            if (edge.Kind == PassageKind.Jump || edge.Kind == PassageKind.Drop)
            {
                scan.TraversalCandidates[(node.Key, scan.Graph.Nodes[edge.To].Key)] = edge;
            }
        }
    }

    /// <summary>Publishes only a fully decoded private result into its matching current session.</summary>
    public static void Restore(DungeonSession session, CachedGeometry geometry, ScanResult scan)
    {
        session.Scan = scan;
        session.Layout = geometry.Layout;
        session.Floors = geometry.Layout.Levels;
        session.NavigationReady = geometry.Complete;
        session.HasPartialGeometry = !geometry.Complete;
        session.GeometryNeedsScan = !geometry.Complete || session.GeometryDirty;
        session.GeometryRevision++;
        if (!session.Area.ObservedEntrance && geometry.Entrance.HasValue)
        {
            session.Area.Entrance = geometry.Entrance;
            session.Area.ObservedEntrance = true;
        }
    }
}
