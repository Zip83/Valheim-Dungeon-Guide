using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGuide;

/// <summary>Owns the explicitly opened, native-styled legend dialog and its scrollable content.</summary>
internal sealed class MapLegendView : IDisposable
{
    private const float Margin = 16;
    private const float HeaderHeight = 56;
    private const float FooterHeight = 44;
    private const float ScrollbarSpace = 20;
    private readonly Transform _parent;
    private readonly GameUiStyle _style;
    private readonly GuideText _text;
    private RectTransform _root;
    private RectTransform _dialog;
    private ScrollRect _scroll;
    private List<MapLegendRow> _rows;
    private bool _available;
    private bool _disposed;
    private Vector2 _availableSize = new(-1, -1);
    private float _rowWidth = -1;
    private float _contentHeight;

    /// <summary>Whether the player explicitly opened the legend in the current large dungeon map.</summary>
    public bool IsOpen => _root && _root.gameObject.activeSelf;

    /// <summary>Remembers borrowed Valheim visuals; UI creation waits for the first explicit open request.</summary>
    public MapLegendView(Transform parent, GameUiStyle style, GuideText text)
    {
        _parent = parent;
        _style = style;
        _text = text;
    }

    private void EnsureCreated()
    {
        if (_root)
        {
            return;
        }

        _root = FullRect("DungeonMapLegend", _parent);
        CreateBackdrop();
        _dialog = CreateDialog(_style);
        CreateHeader(_style, _text);
        _scroll = CreateScroll();
        _rows = CreateRows(_scroll.content, _style, _text);
        CreateFooter(_style, _text);
        _availableSize = new Vector2(-1, -1);
        _rowWidth = -1;
        _root.gameObject.SetActive(false);
    }

    /// <summary>Closes the dialog when the map, dungeon view, or active session is no longer available.</summary>
    public void SetAvailable(bool available)
    {
        _available = available && !_disposed;
        if (!_available)
        {
            Close();
        }

        if (IsOpen)
        {
            RefreshLayout();
        }
    }

    /// <summary>Opens only on request, or closes an open legend without changing map selection.</summary>
    public void Toggle()
    {
        if (!_available || IsOpen)
        {
            Close();
            return;
        }

        EnsureCreated();
        _root.SetAsLastSibling();
        _root.gameObject.SetActive(true);
        RefreshLayout();
        _scroll.StopMovement();
        _scroll.content.anchoredPosition = Vector2.zero;
        _scroll.verticalNormalizedPosition = 1;
    }

    /// <summary>Hides the dialog and stops scroll inertia; reopening starts at the first legend row.</summary>
    public void Close()
    {
        _scroll?.StopMovement();
        if (_root)
        {
            _root.gameObject.SetActive(false);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _available = false;
        _disposed = true;
        if (_root)
        {
            UnityEngine.Object.Destroy(_root.gameObject);
        }

        _root = null;
        _dialog = null;
        _scroll = null;
        _rows = null;
    }

    private void RefreshLayout()
    {
        var available = ((RectTransform)_root.parent).rect.size;
        if (_availableSize == available)
        {
            return;
        }

        _availableSize = available;
        var width = Mathf.Max(1, Mathf.Min(560, available.x - Margin * 2));
        LayoutRows(Mathf.Max(1, width - Margin * 2 - ScrollbarSpace));
        var height = Mathf.Max(1, Mathf.Min(_contentHeight + HeaderHeight + FooterHeight, available.y - Margin * 2));
        _dialog.sizeDelta = new Vector2(width, height);
        _scroll.content.sizeDelta = new Vector2(0, _contentHeight);
    }

    private void LayoutRows(float width)
    {
        if (Mathf.Approximately(_rowWidth, width))
        {
            return;
        }

        _rowWidth = width;
        var top = 4f;
        foreach (var row in _rows)
        {
            top += row.Layout(width, top);
        }

        _contentHeight = top + 4;
    }

    private static List<MapLegendRow> CreateRows(Transform parent, GameUiStyle style, GuideText text)
    {
        var rows = new List<MapLegendRow>();
        foreach (var entry in MapLegendCatalog.Entries(text))
        {
            rows.Add(new MapLegendRow(parent, style, entry.Color, entry.Label));
        }

        return rows;
    }

    private void CreateBackdrop()
    {
        var backdrop = FullRect("LegendBackdrop", _root);
        var image = backdrop.gameObject.AddComponent<Image>();
        image.color = new Color(0, 0, 0, 0.55f);
        var button = backdrop.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(Close);
    }

    private RectTransform CreateDialog(GameUiStyle style)
    {
        var dialog = NativeUi.Rect("LegendPanel", _root);
        dialog.anchorMin = dialog.anchorMax = dialog.pivot = new Vector2(0.5f, 0.5f);
        dialog.anchoredPosition = Vector2.zero;
        NativeUi.Panel(dialog, style);
        return dialog;
    }

    private void CreateHeader(GameUiStyle style, GuideText text)
    {
        var title = NativeUi.Label(_dialog, style, text.Legend, new Vector2(Margin, -12), Vector2.zero, 21);
        title.alignment = TMPro.TextAlignmentOptions.MidlineLeft;
        title.rectTransform.anchorMax = Vector2.one;
        title.rectTransform.sizeDelta = new Vector2(-Margin * 2 - 112, 32);

        var close = NativeUi.Button(_dialog, style, text.LegendClose, Close);
        var rect = (RectTransform)close.transform.parent;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-Margin, -12);
        rect.sizeDelta = new Vector2(104, 32);
    }

    private ScrollRect CreateScroll()
    {
        var area = FullRect("LegendScroll", _dialog);
        area.offsetMin = new Vector2(Margin, FooterHeight);
        area.offsetMax = new Vector2(-Margin, -HeaderHeight);
        var viewport = FullRect("Viewport", area);
        viewport.offsetMax = new Vector2(-ScrollbarSpace, 0);
        viewport.gameObject.AddComponent<RectMask2D>();
        viewport.gameObject.AddComponent<Image>().color = Color.clear;
        var content = NativeUi.Rect("LegendRows", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;

        var scroll = area.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28;
        scroll.verticalScrollbar = CreateScrollbar(area);
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        return scroll;
    }

    private static Scrollbar CreateScrollbar(RectTransform parent)
    {
        var track = NativeUi.Rect("Scrollbar", parent);
        track.anchorMin = new Vector2(1, 0);
        track.anchorMax = Vector2.one;
        track.pivot = new Vector2(1, 0.5f);
        track.sizeDelta = new Vector2(10, 0);
        track.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.45f);
        var handle = FullRect("Handle", track);
        var image = handle.gameObject.AddComponent<Image>();
        image.color = new Color(0.68f, 0.53f, 0.3f, 0.9f);
        var scrollbar = track.gameObject.AddComponent<Scrollbar>();
        scrollbar.handleRect = handle;
        scrollbar.targetGraphic = image;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };
        return scrollbar;
    }

    private void CreateFooter(GameUiStyle style, GuideText text)
    {
        var footer = NativeUi.Label(_dialog, style, text.LegendScrollHint, new Vector2(Margin, 8), Vector2.zero, 14);
        footer.rectTransform.anchorMin = new Vector2(0, 0);
        footer.rectTransform.anchorMax = new Vector2(1, 0);
        footer.rectTransform.pivot = Vector2.zero;
        footer.rectTransform.sizeDelta = new Vector2(-Margin * 2, 28);
    }

    private static RectTransform FullRect(string name, Transform parent)
    {
        var rect = NativeUi.Rect(name, parent);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }
}
