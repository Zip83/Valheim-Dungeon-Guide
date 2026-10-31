using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Pairs localized map meanings with the renderer's actual shared palette.</summary>
internal static class MapLegendCatalog
{
    /// <summary>Returns floor, marker, and route entries without formatting or missing-font symbols.</summary>
    public static IEnumerable<(Color32 Color, string Label)> Entries(GuideText text) => new[]
    {
        (MapPalette.ExploredFloor, text.LegendExplored),
        (MapPalette.VisitedRoom, text.LegendVisited),
        (MapPalette.Cloud(0, 0), text.LegendFog),
        (MapPalette.FloorOutline, text.LegendWalls),
        (MapPalette.Player, text.LegendPlayer),
        (MapPalette.Route, text.LegendRoute),
        (MapPalette.GrappleRoute, text.LegendGrapple),
        (MapPalette.FloorTransition, text.LegendStairs),
        (MapPalette.Entrance, text.LegendEntrance),
        (MapPalette.Waypoint, text.LegendWaypoint),
        (MapPalette.KnownChest, text.LegendChest),
        (MapPalette.CollectionChest, text.LegendCollection),
        (MapPalette.UnverifiedPassage, text.LegendUnverified)
    };
}
