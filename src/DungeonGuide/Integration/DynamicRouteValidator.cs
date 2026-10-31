using System;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Checks active route passages against current geometry and player traversal capabilities.
/// </summary>
internal sealed class DynamicRouteValidator
{
    private readonly GuideText _text;
    private readonly GeometryPhysics _physics;
    private readonly RouteCursor _cursor;

    /// <summary>
    /// Uses collision queries and route progress to recheck upcoming passages.
    /// </summary>
    public DynamicRouteValidator(GeometryPhysics physics, RouteCursor cursor, GuideText text)
    {
        _text = text;
        _physics = physics;
        _cursor = cursor;
    }

    /// <summary>
    /// Revalidates nearby route passages and returns a player instruction when interaction or traversal is blocked.
    /// </summary>
    public string Inspect(DungeonSession session, NavigationCoordinator navigation, Player player)
    {
        var route = navigation.Route;
        if (route.Count < 2)
        {
            return null;
        }

        var graph = session.Scan.Graph;
        var near = _cursor.Nearest(graph, route, UnityPoints.P(player.transform.position));
        var next = Math.Min(near + 1, route.Count - 1);
        if (near == next)
        {
            return null;
        }

        if (navigation.IsGrapplePassage(route[near], route[next]))
        {
            return null;
        }

        var a = graph.Nodes[route[near]];
        var b = graph.Nodes[route[next]];
        var edge = a.Edges.FirstOrDefault(e => e.To == route[next]);
        if (_physics.Visible(UnityPoints.V(a.Position), UnityPoints.V(b.Position)))
        {
            return null;
        }

        return BlockedPassage(a, b, edge, route[near]);
    }

    private string BlockedPassage(NavNode a, NavNode b, NavEdge edge, int from)
    {
        if (edge?.Kind == PassageKind.Door)
        {
            return _text.OpenDoor;
        }

        if (edge?.Kind == PassageKind.Breakable)
        {
            return _text.BreakObstacle + edge.Obstacle;
        }

        if (edge?.Kind == PassageKind.Jump || edge?.Kind == PassageKind.Drop)
        {
            return null;
        }

        if (edge != null)
        {
            a.Edges.Remove(edge);
            b.Edges.RemoveAll(e => e.To == from);
        }

        return _text.RouteBlocked;
    }
}
