using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGuide;

/// <summary>Creates guide-owned UI controls using shared Valheim visuals and fresh input handlers.</summary>
internal static class NativeUi
{
    /// <summary>Creates a named UI rectangle under the supplied parent.</summary>
    public static RectTransform Rect(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        var rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    /// <summary>Frames a guide-owned panel with a native sprite without cloning game logic.</summary>
    public static void Panel(RectTransform rect, GameUiStyle style, bool interactive = true)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = style.PanelSprite;
        image.material = style.PanelMaterial;
        image.type = style.PanelType;
        image.color = style.PanelColor;
        image.raycastTarget = interactive;
    }

    /// <summary>Creates a localized, non-interactive caption using the loaded game font.</summary>
    public static TMP_Text Label(Transform parent, GameUiStyle style, string text, Vector2 position,
        Vector2 size, int fontSize = 17)
    {
        var rect = Rect("GuideText", parent);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = style.Font;
        label.fontSharedMaterial = style.FontMaterial;
        label.fontSize = label.fontSizeMax = fontSize;
        label.fontSizeMin = 12;
        label.enableAutoSizing = true;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.text = text;
        label.color = style.TextColor;
        label.raycastTarget = false;
        label.alignment = TextAlignmentOptions.Center;
        return label;
    }

    /// <summary>Creates a button containing only the guide's action and native image/text styling.</summary>
    public static TMP_Text Button(Transform parent, GameUiStyle style, string text, Action click)
    {
        var rect = Rect("GuideButton", parent);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = style.ButtonSprite;
        image.material = style.ButtonMaterial;
        image.type = style.ButtonType;
        image.color = style.ButtonColor;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.colors = style.Colors;
        button.spriteState = style.Sprites;
        button.transition = style.Transition;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => click());
        var caption = Label(rect, style, text, Vector2.zero, Vector2.zero);
        caption.rectTransform.anchorMin = Vector2.zero;
        caption.rectTransform.anchorMax = Vector2.one;
        caption.rectTransform.sizeDelta = new Vector2(-8, -2);
        return caption;
    }
}
