using System;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Formats player-relative navigation directions and destination labels.
/// </summary>
internal sealed class NavigationInstructionFormatter
{
    private readonly GuideText _text;
    private readonly RouteCursor _cursor;
    private readonly FloorNumbering _numbering;

    /// <summary>
    /// Uses route progress and entrance-relative floor labels to format instructions.
    /// </summary>
    public NavigationInstructionFormatter(RouteCursor cursor, FloorNumbering numbering, GuideText text)
    {
        _text = text;
        _cursor = cursor;
        _numbering = numbering;
    }

    /// <summary>
    /// Formats the current navigation destination and floor label.
    /// </summary>
    public string Title(DungeonSession session, NavigationCoordinator navigation)
    {
        switch (navigation.Goal)
        {
            case NavigationGoal.Explore:
                return _text.Explore;
            case NavigationGoal.Chest:
                return (navigation.Chest.collect ? _text.CollectChest : _text.ChestDestination)
                + navigation.Chest.label;
            case NavigationGoal.Waypoint:
                return navigation.Waypoint.label;
            case NavigationGoal.Entrance:
                return session.Area.Entrance.HasValue ? _text.EntranceRoom : _text.UnknownEntrance;
            case NavigationGoal.Floor:
                return FloorTitle(session, navigation);
            default:
                return "";
        }
    }

    /// <summary>
    /// Formats character-relative directions, floor transitions, and relevant blockage instructions.
    /// </summary>
    public string Instruction(DungeonSession session, NavigationCoordinator navigation, Player player, string blockedHint)
    {
        if (!string.IsNullOrEmpty(blockedHint))
        {
            return blockedHint;
        }

        var route = navigation.Route;
        if (route.Count == 0)
        {
            return navigation.SearchingGrapple ? _text.SearchingGrapple : StatusInstruction(session, navigation.Status);
        }

        var graph = session.Scan.Graph;
        var near = _cursor.Nearest(graph, route, UnityPoints.P(player.transform.position));
        var next = Math.Min(near + 1, route.Count - 1);
        var a = graph.Nodes[route[near]];
        var b = graph.Nodes[route[next]];
        var edge = a.Edges.FirstOrDefault(e => e.To == route[next]);
        if (near == next && Point.Distance(UnityPoints.P(player.transform.position), b.Position) < 2)
        {
            return _text.NearDestination;
        }

        var action = PassageInstruction(navigation, route[near], route[next], edge);
        if (action != null)
        {
            return action;
        }

        var leg = RouteInstructionLookahead.Measure(graph, route, near, UnityPoints.P(player.transform.position),
            navigation.IsGrapplePassage);
        var direction = DirectionInstruction(player, b.Position, leg.Distance);
        return navigation.RequiresGrapple ? _text.GrappleRequired + _text.Separator + direction : direction;
    }

    private string PassageInstruction(NavigationCoordinator navigation, int from, int to, NavEdge edge)
    {
        if (navigation.IsGrapplePassage(from, to))
        {
            return _text.GrappleInstruction;
        }

        return edge?.Kind switch
        {
            PassageKind.Jump => _text.JumpInstruction,
            PassageKind.Drop => _text.DropInstruction,
            _ => null
        };
    }

    private string FloorTitle(DungeonSession session, NavigationCoordinator navigation)
    {
        var direction = navigation.FloorDirection > 0 ? _text.NavigateUp : _text.NavigateDown;
        return navigation.DestinationFloor < 0 ? direction : direction + _text.Separator
            + _numbering.Label(session.Floors, navigation.DestinationFloor, session.Area.Entrance?.y);
    }

    private string StatusInstruction(DungeonSession session, NavigationStatus status)
    {
        if (session.HasPartialGeometry && session.Scanning)
        {
            return _text.PreparingRoute;
        }

        switch (status)
        {
            case NavigationStatus.OffGraph:
                return _text.ApproachPassage;
            case NavigationStatus.ExplorationExhausted:
                return _text.ExplorationExhausted;
            case NavigationStatus.FloorLimit:
                return _text.NoAdjacentFloor;
            case NavigationStatus.NoRoute:
                return _text.NoVerifiedRoute;
            default:
                return session.Scanning ? _text.PreparingRoute : "";
        }
    }

    private string DirectionInstruction(Player player, Point destination, float distance)
    {
        var delta = UnityPoints.V(destination) - player.transform.position;
        var angle = Vector3.SignedAngle(player.transform.forward, new Vector3(delta.x, 0, delta.z), Vector3.up);
        var turn = Mathf.Abs(angle) < 25
            ? _text.Straight
            : Mathf.Abs(angle) > 145 ? _text.TurnAround : angle > 0 ? _text.Right : _text.Left;
        return turn + (delta.y > 0.5f ? _text.UpIndicator : delta.y < -0.5f ? _text.DownIndicator : "")
            + _text.Separator
            + distance.ToString("0.0")
            + " m";
    }
}
