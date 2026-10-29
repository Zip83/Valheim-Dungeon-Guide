using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Defines the colors shared by dungeon map controls, floor geometry, and navigation markers.
/// </summary>
internal static class MapPalette
{
    internal static readonly Color Panel = new Color(0.06f, 0.08f, 0.1f, 0.97f);
    internal static readonly Color MapBackground = new Color(0.04f, 0.05f, 0.065f, 1);
    internal static readonly Color Button = new Color(0.19f, 0.23f, 0.27f, 0.98f);
    internal static readonly Color32 TextureBackground = new Color32(17, 22, 27, 255);
    internal static readonly Color32 ExploredFloor = new Color32(139, 172, 153, 255);
    internal static readonly Color32 ScannedFloor = new Color32(91, 109, 121, 255);
    internal static readonly Color32 Route = new Color32(255, 222, 65, 255);
    internal static readonly Color32 FloorTransition = new Color32(105, 198, 255, 255);
    internal static readonly Color32 UnverifiedPassage = new Color32(180, 126, 208, 255);
    internal static readonly Color32 CollectionChest = new Color32(255, 214, 50, 255);
    internal static readonly Color32 KnownChest = new Color32(198, 134, 62, 255);
    internal static readonly Color32 Entrance = new Color32(45, 215, 223, 255);
    internal static readonly Color32 Player = new Color32(255, 255, 255, 255);
}
