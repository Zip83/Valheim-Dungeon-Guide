using System;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGuide;

/// <summary>
/// Creates the focused Unity UI elements used by dungeon map controls.
/// </summary>
internal static class NativeUi
{
    private static Font s_font;

    /// <summary>
    /// Creates a named UI rectangle under the supplied parent.
    /// </summary>
    public static RectTransform Rect(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    /// <summary>
    /// Creates a non-interactive label with the configured native map styling.
    /// </summary>
    public static Text Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize = 16)
    {
        if (!s_font)
        {
            s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        var rect = Rect("GuideText", parent);
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var label = rect.gameObject.AddComponent<Text>();
        label.font = s_font;
        label.fontSize = fontSize;
        label.text = text;
        label.color = Color.white;
        label.raycastTarget = false;
        label.alignment = TextAnchor.MiddleCenter;
        return label;
    }

    /// <summary>
    /// Creates a button and label that invoke the supplied user command.
    /// </summary>
    public static Text Button(Transform parent, string text, float x, float y, float width, Action click)
    {
        var rect = Rect("GuideButton", parent);
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, 28);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = MapPalette.Button;
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => click());
        return Label(rect, text, Vector2.zero, new Vector2(width, 28), 15);
    }
}
