using System;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGuide;

/// <summary>Reads shared visual assets from loaded Valheim controls without cloning their behaviours.</summary>
internal sealed class GameUiStyle
{
    private GameUiStyle(Button source, Image panel)
    {
        var image = source.targetGraphic as Image;
        var caption = source.GetComponentInChildren<TMP_Text>(true);
        if (!image || !caption || !caption.font || !image.sprite)
        {
            throw new InvalidOperationException("Valheim button image/font assets are unavailable.");
        }

        ButtonSprite = image.sprite;
        ButtonMaterial = image.material;
        ButtonType = image.type;
        ButtonColor = image.color;
        Colors = source.colors;
        Sprites = source.spriteState;
        Transition = source.transition == Selectable.Transition.SpriteSwap
            ? Selectable.Transition.SpriteSwap : Selectable.Transition.ColorTint;
        Font = caption.font;
        FontMaterial = caption.fontSharedMaterial;
        TextColor = caption.color;
        PanelSprite = panel && panel.sprite ? panel.sprite : image.sprite;
        PanelMaterial = panel && panel.sprite ? panel.material : image.material;
        PanelColor = panel && panel.sprite ? panel.color : image.color;
        PanelType = panel && panel.sprite ? panel.type : image.type;
    }

    /// <summary>Native sliced button artwork.</summary>
    public Sprite ButtonSprite
    {
        get;
    }

    /// <summary>Shared native button material; never modified by the guide.</summary>
    public Material ButtonMaterial
    {
        get;
    }

    /// <summary>Original button sprite rendering mode.</summary>
    public Image.Type ButtonType
    {
        get;
    }

    /// <summary>Original image tint before selectable-state animation.</summary>
    public Color ButtonColor
    {
        get;
    }

    /// <summary>Native normal, highlighted, pressed, selected, and disabled colours.</summary>
    public ColorBlock Colors
    {
        get;
    }

    /// <summary>Native selectable-state artwork.</summary>
    public SpriteState Sprites
    {
        get;
    }

    /// <summary>Safe image-only selectable transition, without copying game animators or callbacks.</summary>
    public Selectable.Transition Transition
    {
        get;
    }

    /// <summary>Loaded game font, including its native fallback configuration.</summary>
    public TMP_FontAsset Font
    {
        get;
    }

    /// <summary>Shared native font material; never modified by the guide.</summary>
    public Material FontMaterial
    {
        get;
    }

    /// <summary>Native caption colour.</summary>
    public Color TextColor
    {
        get;
    }

    /// <summary>Native enclosing panel frame.</summary>
    public Sprite PanelSprite
    {
        get;
    }

    /// <summary>Shared enclosing panel material.</summary>
    public Material PanelMaterial
    {
        get;
    }

    /// <summary>Native enclosing frame rendering mode.</summary>
    public Image.Type PanelType
    {
        get;
    }

    /// <summary>Native panel tint.</summary>
    public Color PanelColor
    {
        get;
    }

    /// <summary>Finds a real loaded Valheim button and extracts only its visual data.</summary>
    public static GameUiStyle Read()
    {
        var button = FindButton();
        return new GameUiStyle(button, FindPanel(button));
    }

    /// <summary>Returns the native map canvas so optional panels inherit Valheim's UI scale.</summary>
    public static Transform MapCanvas()
    {
        var image = Field<RawImage>(Minimap.instance, "m_mapImageSmall");
        var canvas = image ? image.GetComponentInParent<Canvas>() : null;
        if (!canvas)
        {
            throw new InvalidOperationException("Valheim map canvas was not found.");
        }

        return canvas.rootCanvas.transform;
    }

    private static bool Usable(Button button)
    {
        if (!button)
        {
            return false;
        }

        var image = button.targetGraphic as Image;
        var label = button.GetComponentInChildren<TMP_Text>(true);
        return image && image.sprite && label && label.font;
    }

    private static T Field<T>(object source, string name) where T : class => source?.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(source) as T;

    private static Button FindButton()
    {
        var button = Field<Button>(Menu.instance, "m_continueButton");
        if (!Usable(button))
        {
            button = Field<Button>(InventoryGui.instance, "m_takeAllButton");
        }
        if (!Usable(button))
        {
            throw new InvalidOperationException("Valheim menu/inventory button template was not found.");
        }

        return button;
    }

    private static Image FindPanel(Button button)
    {
        Image panel = null;
        for (var parent = button.transform.parent; parent && !panel; parent = parent.parent)
        {
            var candidate = parent.GetComponent<Image>();
            if (candidate && candidate.sprite)
            {
                panel = candidate;
            }
        }

        return panel;
    }
}
