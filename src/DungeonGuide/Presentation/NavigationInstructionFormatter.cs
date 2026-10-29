using System;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Formats player-relative navigation directions and destination labels.
/// </summary>
internal sealed class NavigationInstructionFormatter
{
    private readonly RouteCursor _cursor;
    private readonly FloorNumbering _numbering;

    /// <summary>
    /// Uses route progress and entrance-relative floor labels to format instructions.
    /// </summary>
    public NavigationInstructionFormatter(RouteCursor cursor, FloorNumbering numbering)
    {
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
                return GuideText.Explore;
            case NavigationGoal.Chest:
                return (navigation.Chest.collect ? GuideText.CollectChest : GuideText.ChestDestination)
                + navigation.Chest.label;
            case NavigationGoal.Entrance:
                return session.Area.Entrance.HasValue ? GuideText.EntranceRoom : GuideText.UnknownEntrance;
            case NavigationGoal.Floor:
                return _numbering.Label(session.Floors, navigation.DestinationFloor, session.Area.Entrance?.y);
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
            switch (navigation.Status)
            {
                case NavigationStatus.OffGraph:
                    return GuideText.ApproachPassage;
                case NavigationStatus.ExplorationExhausted:
                    return GuideText.ExplorationExhausted;
                case NavigationStatus.NoRoute:
                    return GuideText.NoVerifiedRoute;
                default:
                    return session.Scanning ? GuideText.PreparingRoute : "";
            }
        }

        var graph = session.Scan.Graph;
        var near = _cursor.Nearest(graph, route, UnityPoints.P(player.transform.position));
        var next = Math.Min(near + 1, route.Count - 1);
        var a = graph.Nodes[route[near]];
        var b = graph.Nodes[route[next]];
        var edge = a.Edges.FirstOrDefault(e => e.To == route[next]);
        if (near == next && Point.Distance(UnityPoints.P(player.transform.position), b.Position) < 2)
        {
            return GuideText.NearDestination;
        }

        if (edge?.Kind == PassageKind.Jump)
        {
            return GuideText.JumpInstruction;
        }

        if (edge?.Kind == PassageKind.Drop)
        {
            return GuideText.DropInstruction;
        }

        var delta = UnityPoints.V(b.Position) - player.transform.position;
        var angle = Vector3.SignedAngle(player.transform.forward, new Vector3(delta.x, 0, delta.z), Vector3.up);
        var turn = Mathf.Abs(angle) < 25
            ? GuideText.Straight
            : Mathf.Abs(angle) > 145 ? GuideText.TurnAround : angle > 0 ? GuideText.Right : GuideText.Left;
        return turn + (delta.y > 0.5f ? GuideText.UpIndicator : delta.y < -0.5f ? GuideText.DownIndicator : "")
            + GuideText.Separator
            + delta.magnitude.ToString("0.0")
            + " m";
    }
}
