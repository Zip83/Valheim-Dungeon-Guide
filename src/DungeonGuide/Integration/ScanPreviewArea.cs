using UnityEngine;

namespace DungeonGuide;

/// <summary>Adapts the initial scan neighborhood to Unity sampling bounds without changing dungeon identity.</summary>
internal static class ScanPreviewArea
{
    /// <summary>Restricts first-time explored-mode sampling to the local player's current neighborhood.</summary>
    /// <param name="session">Dungeon whose completed scan, if any, must remain available.</param>
    /// <param name="revealAll">Whether the player requested the complete map immediately.</param>
    /// <returns>A private sampling area, or null when a preview is unnecessary or unavailable.</returns>
    public static DungeonArea Create(DungeonSession session, bool revealAll)
    {
        var player = Player.m_localPlayer;
        if (session.Scan != null || revealAll || !player)
        {
            return null;
        }

        var area = session.Area;
        var region = ScanPreviewRegion.Create(UnityPoints.P(area.Bounds.min), UnityPoints.P(area.Bounds.max),
            UnityPoints.P(player.transform.position));
        if (region == null)
        {
            return null;
        }

        var bounds = new Bounds();
        bounds.SetMinMax(UnityPoints.V(region.Minimum), UnityPoints.V(region.Maximum));
        return new DungeonArea
        {
            Id = area.Id,
            Label = area.Label,
            IsManual = area.IsManual,
            Bounds = bounds,
            Rooms = area.Rooms,
            Entrance = area.Entrance
        };
    }
}
