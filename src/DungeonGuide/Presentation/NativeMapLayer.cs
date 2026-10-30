using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGuide;

/// <summary>
/// Owns one dungeon texture layer and restores native map chrome when hidden or disposed.
/// </summary>
internal sealed class NativeMapLayer : IDisposable
{
    private readonly RectTransform _root;
    private readonly RawImage _image;
    private readonly RawImage _markers;
    private readonly List<(CanvasGroup, float)> _hidden = new();

    /// <summary>
    /// Transform of the square dungeon floor image inside the native map.
    /// </summary>
    public RectTransform ImageRect => _image.rectTransform;

    /// <summary>
    /// Normalized texture crop currently displayed by this layer.
    /// </summary>
    public Rect Viewport { get; private set; } = new Rect(0, 0, 1, 1);

    /// <summary>
    /// Creates a dungeon image layer and remembers the native chrome state to restore later.
    /// </summary>
    public NativeMapLayer(RawImage original, Texture texture, Texture markers, IEnumerable<GameObject> chrome)
    {
        _root = NativeUi.Rect("DungeonGuideLayer", original.transform);
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        _root.sizeDelta = Vector2.zero;
        var bg = _root.gameObject.AddComponent<Image>();
        bg.color = MapPalette.MapBackground;
        bg.raycastTarget = false;
        var map = NativeUi.Rect("FloorPlan", _root);
        map.anchorMin = map.anchorMax = map.pivot = new Vector2(0.5f, 0.5f);
        _image = map.gameObject.AddComponent<RawImage>();
        _image.texture = texture;
        _image.raycastTarget = false;
        var markerRect = NativeUi.Rect("Markers", map);
        markerRect.anchorMin = Vector2.zero;
        markerRect.anchorMax = Vector2.one;
        markerRect.sizeDelta = Vector2.zero;
        _markers = markerRect.gameObject.AddComponent<RawImage>();
        _markers.texture = markers;
        _markers.raycastTarget = false;
        RememberChrome(chrome);

        _root.gameObject.SetActive(false);
    }

    /// <summary>
    /// Updates dungeon-layer visibility and hides or restores native map chrome.
    /// </summary>
    public void Show(bool enabled, Rect viewport)
    {
        if (!_root || !_image)
        {
            return;
        }

        _root.gameObject.SetActive(enabled);
        Viewport = viewport;
        _image.uvRect = _markers.uvRect = viewport;
        foreach (var item in _hidden)
        {
            if (item.Item1)
            {
                item.Item1.alpha = enabled ? 0 : item.Item2;
            }
        }

        if (enabled)
        {
            var side = Mathf.Min(_root.rect.width, _root.rect.height);
            _image.rectTransform.sizeDelta = new Vector2(side, side);
        }
    }

    /// <summary>
    /// Checks whether a screen point lies within the visible dungeon image.
    /// </summary>
    public bool Contains(Vector2 screen) => _root && _image && _root.gameObject.activeInHierarchy
        && RectTransformUtility.RectangleContainsScreenPoint(_image.rectTransform, screen, null);

    /// <summary>
    /// Converts a screen position to the displayed texture's normalized coordinates.
    /// </summary>
    public Vector2 MouseUV(Vector2 screen)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _image.rectTransform,
            screen,
            null,
            out var local);
        var rect = _image.rectTransform.rect;
        return new Vector2(
            Viewport.x + (local.x - rect.x) / rect.width * Viewport.width,
            Viewport.y + (local.y - rect.y) / rect.height * Viewport.height);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Show(false, Viewport);
        if (_root)
        {
            UnityEngine.Object.Destroy(_root.gameObject);
        }
    }

    private void RememberChrome(IEnumerable<GameObject> chrome)
    {
        foreach (var obj in chrome)
        {
            if (!obj)
            {
                continue;
            }

            var group = obj.GetComponent<CanvasGroup>() ?? obj.AddComponent<CanvasGroup>();
            _hidden.Add((group, group.alpha));
        }
    }
}
