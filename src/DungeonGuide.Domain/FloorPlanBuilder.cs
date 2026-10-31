using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Rasterizes conservative sample footprints and verified short walk connections cooperatively.
/// </summary>
internal sealed class FloorPlanBuilder
{
    /// <summary>
    /// Builds a privately owned grid, yielding after every graph node and raster row.
    /// Callers publish it only after the iterator completes.
    /// </summary>
    public IEnumerable<byte> Build(NavGraph graph, float spacing, FloorPlanRaster raster)
    {
        raster.Graph = graph;
        foreach (var step in RasterizeCoverage(graph, spacing, raster))
        {
            yield return step;
        }

        foreach (var step in TraceBoundaries(raster))
        {
            yield return step;
        }
    }

    private static bool Wall(FloorPlanRaster raster, int owner, int x, int y)
    {
        if (x < 0 || y < 0 || x >= raster.Size || y >= raster.Size)
        {
            return true;
        }

        var other = raster.Owners[y * raster.Size + x];
        return other == 0 || other != owner && !raster.Connections.Contains((owner, other));
    }

    private static IEnumerable<byte> Box(FloorPlanRaster raster, Point a, Point b, float half, int first, int second)
    {
        var x0 = Math.Max(0, (int)Math.Ceiling(raster.X(Math.Min(a.x, b.x) - half) - 0.5f));
        var x1 = Math.Min(raster.Size - 1, (int)Math.Floor(raster.X(Math.Max(a.x, b.x) + half) - 0.5f));
        var y0 = Math.Max(0, (int)Math.Ceiling(raster.Y(Math.Min(a.z, b.z) - half) - 0.5f));
        var y1 = Math.Min(raster.Size - 1, (int)Math.Floor(raster.Y(Math.Max(a.z, b.z) + half) - 0.5f));
        var ax = raster.X(a.x);
        var ay = raster.Y(a.z);
        var bx = raster.X(b.x);
        var by = raster.Y(b.z);
        for (var y = y0; y <= y1; y++)
        {
            yield return 0;
            for (var x = x0; x <= x1; x++)
            {
                var da = (x + 0.5f - ax) * (x + 0.5f - ax) + (y + 0.5f - ay) * (y + 0.5f - ay);
                var db = (x + 0.5f - bx) * (x + 0.5f - bx) + (y + 0.5f - by) * (y + 0.5f - by);
                raster.Owners[y * raster.Size + x] = (da <= db ? first : second) + 1;
            }
        }
    }

    private static IEnumerable<byte> RasterizeCoverage(NavGraph graph, float spacing, FloorPlanRaster raster)
    {
        var half = spacing * 0.44f;
        for (var id = 0; id < graph.Nodes.Count; id++)
        {
            yield return 0;
            var node = graph.Nodes[id];
            if (node.Floor != raster.Floor)
            {
                continue;
            }

            foreach (var step in Box(raster, node.Position, node.Position, half, id, id))
            {
                yield return step;
            }

            foreach (var step in RasterizeConnections(graph, spacing, raster, id, half))
            {
                yield return step;
            }
        }
    }

    private static IEnumerable<byte> RasterizeConnections(
        NavGraph graph,
        float spacing,
        FloorPlanRaster raster,
        int id,
        float half)
    {
        var node = graph.Nodes[id];
        var feature = false;
        foreach (var edge in node.Edges)
        {
            var other = graph.Nodes[edge.To];
            feature |= other.Floor != node.Floor || StairMarkerPolicy.IsTransition(node, other, edge)
                || edge.Kind == PassageKind.Breakable
                || edge.Kind == PassageKind.Jump || edge.Kind == PassageKind.Drop;
            if (!CanBridge(node, other, edge, spacing))
            {
                continue;
            }

            raster.Connections.Add((id + 1, edge.To + 1));
            raster.Connections.Add((edge.To + 1, id + 1));
            if (id < edge.To)
            {
                foreach (var step in Box(raster, node.Position, other.Position, half, id, edge.To))
                {
                    yield return step;
                }
            }
        }

        if (feature)
        {
            raster.Features.Add(id);
        }
    }

    private static IEnumerable<byte> TraceBoundaries(FloorPlanRaster raster)
    {
        for (var y = 0; y < raster.Size; y++)
        {
            yield return 0;
            for (var x = 0; x < raster.Size; x++)
            {
                var index = y * raster.Size + x;
                var owner = raster.Owners[index];
                raster.Boundary[index] = owner != 0 && (Wall(raster, owner, x - 1, y)
                    || Wall(raster, owner, x + 1, y) || Wall(raster, owner, x, y - 1)
                    || Wall(raster, owner, x, y + 1));
            }
        }
    }

    private static bool CanBridge(NavNode node, NavNode other, NavEdge edge, float spacing)
    {
        var dx = Math.Abs(other.Position.x - node.Position.x);
        var dz = Math.Abs(other.Position.z - node.Position.z);
        return !(other.Floor != node.Floor || !edge.Verified || edge.Kind != PassageKind.Walk
            || dx + dz > spacing * 1.1f || dx + dz < spacing * 0.5f
            || Math.Min(dx, dz) > spacing * 0.1f);
    }
}
