using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Defines the colors shared by dungeon map controls, floor geometry, and navigation markers.
/// </summary>
internal static class MapPalette
{
    internal static readonly Color MapBackground = new Color(0.13f, 0.11f, 0.08f, 1);
    internal static readonly Color32 TextureBackground = new Color32(17, 22, 27, 255);
    internal static readonly Color32 ExploredFloor = new Color32(91, 109, 121, 255);
    internal static readonly Color32 FloorOutline = new Color32(63, 51, 35, 255);
    internal static readonly Color32 VisitedRoom = new Color32(124, 177, 117, 255);
    internal static readonly Color32 ScannedFloor = new Color32(91, 109, 121, 255);
    internal static readonly Color32 GrappleRoute = new Color32(255, 132, 45, 255);
    internal static readonly Color32 RouteOutline = new Color32(25, 20, 15, 255);
    internal static readonly Color32 Route = new Color32(255, 222, 65, 255);
    internal static readonly Color32 FloorTransition = new Color32(105, 198, 255, 255);
    internal static readonly Color32 FloorTransitionOutline = new Color32(10, 24, 38, 255);
    internal static readonly Color32 UnverifiedPassage = new Color32(180, 126, 208, 255);
    internal static readonly Color32 CollectionChest = new Color32(255, 214, 50, 255);
    internal static readonly Color32 KnownChest = new Color32(198, 134, 62, 255);
    internal static readonly Color32 Entrance = new Color32(45, 215, 223, 255);
    internal static readonly Color32 Waypoint = new Color32(255, 130, 175, 255);
    internal static readonly Color32 Player = new Color32(255, 255, 255, 255);

    /// <summary>Generates dark charcoal cloud shading independently of hidden dungeon geometry.</summary>
    public static Color32 Cloud(int x, int y)
    {
        var gx = x / 64;
        var gy = y / 64;
        var tx = x % 64 / 64f;
        var ty = y % 64 / 64f;
        tx = tx * tx * (3 - 2 * tx);
        ty = ty * ty * (3 - 2 * ty);
        var noise = Mathf.Lerp(Mathf.Lerp(Noise(gx, gy), Noise(gx + 1, gy), tx),
            Mathf.Lerp(Noise(gx, gy + 1), Noise(gx + 1, gy + 1), tx), ty);
        return new Color32((byte)(23 + noise * 8), (byte)(29 + noise * 8), (byte)(35 + noise * 8), 255);
    }

    private static float Noise(int x, int y) => ((x * 73856093 ^ y * 19349663) & 255) / 255f;
}
