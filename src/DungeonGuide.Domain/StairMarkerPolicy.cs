using System;

namespace DungeonGuide;

/// <summary>Identifies supported vertical passage directions independently of architectural floor numbering.</summary>
internal static class StairMarkerPolicy
{
    /// <summary>Includes verified stairs within a floor and ordinary connections crossing physical floor labels.</summary>
    public static bool IsTransition(NavNode from, NavNode to, NavEdge edge) =>
        edge.Verified && (edge.Kind == PassageKind.Stairs || edge.Kind == PassageKind.Walk && from.Floor != to.Floor)
        && Math.Abs(to.Position.y - from.Position.y) > 0.1f;

    /// <summary>Reports both directions at landings without hiding a second stair behind the first edge.</summary>
    public static (bool Up, bool Down) Directions(NavGraph graph, int id)
    {
        var node = graph.Nodes[id];
        var up = false;
        var down = false;
        foreach (var edge in node.Edges)
        {
            var other = graph.Nodes[edge.To];
            if (IsTransition(node, other, edge))
            {
                up |= other.Position.y > node.Position.y;
                down |= other.Position.y < node.Position.y;
            }
        }

        return (up, down);
    }
}
