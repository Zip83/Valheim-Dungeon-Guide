using TMPro;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Measures and arranges independent shortcut groups with readable wrapping and vertical separation.</summary>
internal sealed class MapShortcutHintsView
{
    private const float GroupGap = 10;
    private readonly TMP_Text[] _groups;
    private float _width = -1;
    private float _height;

    /// <summary>Guide-owned rectangle containing non-interactive shortcut captions.</summary>
    public RectTransform Root
    {
        get;
    }

    /// <summary>Creates native-font groups for chest actions, saved locations and map mouse actions.</summary>
    public MapShortcutHintsView(Transform parent, GameUiStyle style, GuideText text)
    {
        Root = NativeUi.Rect("ShortcutHints", parent);
        Root.anchorMin = Root.anchorMax = Root.pivot = new Vector2(0, 1);
        _groups = new[]
        {
            CreateGroup("ChestShortcuts", style, text.ChestControls),
            CreateGroup("LocationShortcuts", style, text.WaypointControls),
            CreateGroup("MapShortcuts", style, text.WaypointMapControls)
        };
    }

    /// <summary>Fits all wrapped groups to the available width and returns their complete height.</summary>
    public float Layout(float width)
    {
        width = Mathf.Max(1, width);
        if (_width != width)
        {
            _width = width;
            _height = ArrangeGroups(width);
            Root.sizeDelta = new Vector2(width, _height);
        }

        return _height;
    }

    private TMP_Text CreateGroup(string name, GameUiStyle style, string text)
    {
        var label = NativeUi.Label(Root, style, text, Vector2.zero, Vector2.zero, 14);
        label.name = name;
        label.enableAutoSizing = false;
        label.richText = false;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        label.lineSpacing = 4;

        return label;
    }

    private float ArrangeGroups(float width)
    {
        var top = 0f;
        foreach (var group in _groups)
        {
            var height = Mathf.Max(22, group.GetPreferredValues(group.text, width, 0).y + 6);
            group.rectTransform.anchoredPosition = new Vector2(0, -top);
            group.rectTransform.sizeDelta = new Vector2(width, height);
            top += height + GroupGap;
        }

        return top - GroupGap;
    }
}
