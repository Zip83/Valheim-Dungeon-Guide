using System;

namespace DungeonGuide;

/// <summary>Identifies launch samples at an edge of the currently walkable floor rather than room interiors.</summary>
internal static class GrappleLaunchBoundary
{
    /// <summary>A sample is an edge when at least one adjacent cardinal step lacks a permitted ordinary passage.</summary>
    public static bool Contains(NavGraph graph, int source, float spacing, RoutePolicy policy, Predicate<int> member)
    {
        var origin = graph.Nodes[source].Position;
        var directions = 0;
        foreach (var edge in graph.Nodes[source].Edges)
        {
            if (!member(edge.To) || !policy.Allows(edge) || edge.Kind == PassageKind.Jump || edge.Kind == PassageKind.Drop)
            {
                continue;
            }

            var next = graph.Nodes[edge.To].Position;
            var dx = (int)Math.Round((next.x - origin.x) / spacing);
            var dz = (int)Math.Round((next.z - origin.z) / spacing);
            if (Math.Abs(next.y - origin.y) <= spacing && Math.Abs(dx) + Math.Abs(dz) == 1)
            {
                directions |= (dx, dz) switch
                {
                    (-1, 0) => 1,
                    (1, 0) => 2,
                    (0, -1) => 4,
                    (0, 1) => 8,
                    _ => 0
                };
            }
        }

        return directions != 15;
    }
}
