using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGuide;

/// <summary>Displays one color sample and measures its localized caption without font glyph placeholders.</summary>
internal sealed class MapLegendRow
{
    private const float CaptionOffset = 40;
    private readonly RectTransform _swatch;
    private readonly TMP_Text _caption;

    /// <summary>Creates a non-interactive color sample and a wrapping native-font caption.</summary>
    public MapLegendRow(Transform parent, GameUiStyle style, Color32 color, string caption)
    {
        _swatch = NativeUi.Rect("LegendColor", parent);
        _swatch.anchorMin = _swatch.anchorMax = _swatch.pivot = new Vector2(0, 1);
        _swatch.sizeDelta = new Vector2(26, 22);
        var border = _swatch.gameObject.AddComponent<Image>();
        border.color = new Color(0.04f, 0.04f, 0.04f, 1);
        border.raycastTarget = false;
        var fill = NativeUi.Rect("Color", _swatch);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = new Vector2(3, 3);
        fill.offsetMax = new Vector2(-3, -3);
        var image = fill.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        _caption = NativeUi.Label(parent, style, caption, Vector2.zero, Vector2.zero, 17);
        _caption.enableAutoSizing = false;
        _caption.richText = false;
        _caption.alignment = TextAlignmentOptions.MidlineLeft;
        _caption.textWrappingMode = TextWrappingModes.Normal;
        _caption.overflowMode = TextOverflowModes.Overflow;
    }

    /// <summary>Fits a whole caption, centers the swatch, and returns row height including separation.</summary>
    public float Layout(float width, float top)
    {
        var textWidth = Mathf.Max(1, width - CaptionOffset - 4);
        var height = Mathf.Max(32, _caption.GetPreferredValues(_caption.text, textWidth, 0).y + 8);
        _caption.rectTransform.anchoredPosition = new Vector2(CaptionOffset, -top);
        _caption.rectTransform.sizeDelta = new Vector2(textWidth, height);
        _swatch.anchoredPosition = new Vector2(4, -top - (height - 22) / 2);
        return height + 4;
    }
}
