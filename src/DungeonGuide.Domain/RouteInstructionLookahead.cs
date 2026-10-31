using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonGuide;

/// <summary>Measures the current route leg through aligned ordinary samples up to the next maneuver.</summary>
internal static class RouteInstructionLookahead
{
    /// <summary>Returns the leg endpoint and remaining path length without crossing an action or floor boundary.</summary>
    /// <param name="graph">Current navigation graph.</param>
    /// <param name="route">Ordered node indices for the selected destination.</param>
    /// <param name="start">Current route-list cursor.</param>
    /// <param name="player">Current player position in metres.</param>
    /// <param name="special">Optional predicate identifying hook passages outside ordinary graph edges.</param>
    /// <returns>Route-list endpoint and remaining distance in metres; an empty route returns (-1, 0).</returns>
    public static (int End, float Distance) Measure(NavGraph graph, IReadOnlyList<int> route, int start, Point player,
        Func<int, int, bool> special = null)
    {
        if (route.Count == 0)
        {
            return (-1, 0);
        }

        var end = Math.Min(start + 1, route.Count - 1);
        var distance = Point.Distance(player, graph.Nodes[route[end]].Position);
        while (end + 1 < route.Count && Continues(graph, route, start, end, special))
        {
            distance += Point.Distance(graph.Nodes[route[end]].Position, graph.Nodes[route[end + 1]].Position);
            end++;
        }

        return (end, distance);
    }

    private static bool Continues(NavGraph graph, IReadOnlyList<int> route, int start, int end,
        Func<int, int, bool> special)
    {
        var origin = graph.Nodes[route[start]];
        var current = graph.Nodes[route[end]];
        var initial = origin.Edges.FirstOrDefault(edge => edge.To == route[start + 1]);
        var next = current.Edges.FirstOrDefault(edge => edge.To == route[end + 1]);
        return Ordinary(initial) && Ordinary(next) && initial.Kind == next.Kind
            && current.Floor == origin.Floor
            && special?.Invoke(route[start], route[start + 1]) != true
            && special?.Invoke(route[end], route[end + 1]) != true
            && Aligned(origin.Position, graph.Nodes[route[start + 1]].Position,
                current.Position, graph.Nodes[route[end + 1]].Position);
    }

    private static bool Ordinary(NavEdge edge) =>
        edge != null && edge.Verified && (edge.Kind == PassageKind.Walk || edge.Kind == PassageKind.Stairs);

    private static bool Aligned(Point a, Point b, Point c, Point d)
    {
        var ax = b.x - a.x;
        var az = b.z - a.z;
        var bx = d.x - c.x;
        var bz = d.z - c.z;
        var length = Math.Sqrt((ax * ax + az * az) * (bx * bx + bz * bz));
        return length > 0.0001 && (ax * bx + az * bz) / length >= Math.Cos(25 * Math.PI / 180)
            && VerticalDirection(b.y - a.y) == VerticalDirection(d.y - c.y);
    }

    private static int VerticalDirection(float height) => Math.Abs(height) < 0.1f ? 0 : Math.Sign(height);
}
