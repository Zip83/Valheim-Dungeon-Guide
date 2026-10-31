using TMPro;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Displays the shared map palette in a localized, wrapping legend owned by its parent UI.</summary>
internal sealed class MapLegendView
{
    private readonly TMP_Text _label;

    /// <summary>Creates a non-interactive legend using the same colors as the map renderer.</summary>
    public MapLegendView(Transform parent, GameUiStyle style, GuideText text)
    {
        _label = NativeUi.Label(parent, style, Content(text), Vector2.zero, Vector2.zero, 13);
        _label.enableAutoSizing = false;
        _label.textWrappingMode = TextWrappingModes.Normal;
        _label.overflowMode = TextOverflowModes.Overflow;
        _label.alignment = TextAlignmentOptions.TopLeft;
    }

    /// <summary>Fits the complete legend below its controls without truncating narrow layouts.</summary>
    public float Layout(float width, float top)
    {
        var contentWidth = Mathf.Max(100, width - 16);
        var height = Mathf.Max(24, _label.GetPreferredValues(_label.text, contentWidth, 0).y + 8);
        _label.rectTransform.anchoredPosition = new Vector2(8, -top);
        _label.rectTransform.sizeDelta = new Vector2(contentWidth, height);
        return height;
    }

    private static string Content(GuideText text) => string.Join("   ", new[]
    {
        Entry(MapPalette.ExploredFloor, text.LegendExplored),
        Entry(MapPalette.VisitedRoom, text.LegendVisited),
        Entry(MapPalette.Cloud(0, 0), text.LegendFog),
        Entry(MapPalette.FloorOutline, text.LegendWalls),
        Entry(MapPalette.Player, text.LegendPlayer),
        Entry(MapPalette.Route, text.LegendRoute),
        Entry(MapPalette.FloorTransition, text.LegendStairs),
        Entry(MapPalette.Entrance, text.LegendEntrance),
        Entry(MapPalette.KnownChest, text.LegendChest),
        Entry(MapPalette.CollectionChest, text.LegendCollection),
        Entry(MapPalette.UnverifiedPassage, text.LegendUnverified)
    });

    private static string Entry(Color32 color, string caption) =>
        "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">[]</color> " + caption;
}
