using UnityEngine;

namespace DungeonGuide;

/// <summary>Defines the enlarged player pointer independently of glyph rasterization.</summary>
internal static class PlayerMarkerStyle
{
    internal const int Radius = 17;
    internal const float Tip = 13;
    internal const float Tail = -6;
    internal const float Notch = -2;
    internal const float HalfWidth = 6.5f;
    internal const float OutlineScale = 1.2f;
    internal const float AngleStep = 1;

    internal static readonly Color32 Outline = new(12, 17, 22, 255);
    internal static readonly Color32 TailTint = new(215, 232, 242, 255);
}
