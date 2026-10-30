using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Owns a responsive two-row native toolbar and an explicitly expanded advanced panel.</summary>
internal sealed class MapControlsView : IDisposable
{
    private readonly GuideText _text;
    private readonly FloorNumbering _numbering;
    private readonly MapToolbarState _state = new();
    private readonly RectTransform _details;
    private readonly RectTransform _advanced;
    private readonly TMP_Text _floorLabel;
    private readonly TMP_Text _revealLabel;
    private readonly TMP_Text _worldLabel;
    private readonly TMP_Text _breakLabel;
    private readonly TMP_Text _jumpLabel;
    private readonly TMP_Text _moreLabel;
    private readonly UnityEngine.UI.Button _rescanButton;
    private readonly List<(RectTransform Rect, float Weight)> _first = new();
    private readonly List<(RectTransform Rect, float Weight)> _second = new();
    private readonly List<(RectTransform Rect, float Weight)> _extra = new();
    private readonly RectTransform _hints;

    /// <summary>Root rectangle defining the map area owned by controls and pointer interaction.</summary>
    public RectTransform Root
    {
        get;
    }

    /// <summary>Builds fresh guide controls with native visual assets and independent command callbacks.</summary>
    /// <param name="parent">Native map transform that owns the toolbar.</param>
    /// <param name="numbering">Policy for displaying floor numbers.</param>
    /// <param name="command">Callback receiving guide commands from newly created controls.</param>
    /// <param name="text">Localized control labels.</param>
    /// <param name="style">Visual assets borrowed from the loaded game UI.</param>
    public MapControlsView(Transform parent, FloorNumbering numbering, Action<GuideCommand> command,
        GuideText text, GameUiStyle style)
    {
        _text = text;
        _numbering = numbering;
        Root = CreateRoot(parent, style);
        _details = CreateContainer("DungeonControls", Root);
        _worldLabel = NativeUi.Button(Root, style, text.WorldMap, () => command(GuideCommand.ToggleWorld));
        _first.Add(((RectTransform)_worldLabel.transform.parent, 150));

        (_floorLabel, _moreLabel) = CreateFloorControls(command, style);
        CreateNavigationControls(command, style);

        _advanced = CreateContainer("AdvancedControls", _details);
        (_revealLabel, _rescanButton, _breakLabel, _jumpLabel, _hints) = CreateAdvancedControls(command, style);
        _advanced.gameObject.SetActive(false);
    }

    private static TMP_Text Add(List<(RectTransform Rect, float Weight)> row, Transform parent,
        GameUiStyle style, string caption, float weight, Action command)
    {
        var label = NativeUi.Button(parent, style, caption, command);
        row.Add(((RectTransform)label.transform.parent, weight));
        return label;
    }

    private static void Layout(List<(RectTransform Rect, float Weight)> row, float width, float y)
    {
        var total = 0f;
        foreach (var item in row)
        {
            total += item.Weight;
        }

        var available = Mathf.Max(1, width - 16 - (row.Count - 1) * 4);
        var x = 8f;
        foreach (var item in row)
        {
            var size = available * item.Weight / total;
            item.Rect.anchoredPosition = new Vector2(x, y);
            item.Rect.sizeDelta = new Vector2(size, 30);
            x += size + 4;
        }
    }

    /// <summary>Updates labels, collapse-on-open policy, and layout at the game's current UI scale.</summary>
    public void Refresh(DungeonSession session, MapViewState view, int displayedFloor, bool mapOpen)
    {
        Root.gameObject.SetActive(session != null);
        if (session == null)
        {
            _state.Collapse();
            return;
        }

        _state.SetMapOpen(mapOpen);
        if (!view.Dungeon)
        {
            _state.Collapse();
        }

        _details.gameObject.SetActive(view.Dungeon);
        _advanced.gameObject.SetActive(_state.Expanded);
        RefreshLabels(session, view, displayedFloor);

        LayoutControls(view.Dungeon);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Root)
        {
            UnityEngine.Object.Destroy(Root.gameObject);
        }
    }

    private void RefreshLabels(DungeonSession session, MapViewState view, int displayedFloor)
    {
        _worldLabel.text = view.Dungeon ? _text.WorldMap : _text.DungeonMap;
        _floorLabel.text = displayedFloor < 0 || displayedFloor != view.SelectedFloor ? _text.PreparingMap
            : _numbering.Label(session.Floors, displayedFloor, session.Area.Entrance?.y)
                + (session.HasPartialGeometry ? _text.PreviewSuffix : "");
        _rescanButton.interactable = !session.Scanning;
        _moreLabel.text = _state.Expanded ? _text.Less : _text.More;
        _revealLabel.text = view.RevealAll ? _text.FullDungeon : _text.Explored;
        _breakLabel.text = _text.BreakablePassages + (view.AllowBreakables ? _text.Yes : _text.No);
        _jumpLabel.text = _text.VerifiedJumps + (view.AllowJumps ? _text.Yes : _text.No);
    }

    private void LayoutControls(bool dungeon)
    {
        var parent = Root.parent as RectTransform;
        var width = Mathf.Max(240, Mathf.Min(960, parent.rect.width - 24));
        Root.sizeDelta = new Vector2(dungeon ? width : 180, dungeon ? (_state.Expanded ? 152 : 82) : 46);
        if (dungeon)
        {
            Layout(_first, width, -8);
            Layout(_second, width, -44);
            Layout(_extra, width, -80);
            _hints.anchoredPosition = new Vector2(8, -116);
            _hints.sizeDelta = new Vector2(width - 16, 28);
        }
        else
        {
            var world = (RectTransform)_worldLabel.transform.parent;
            world.anchoredPosition = new Vector2(8, -8);
            world.sizeDelta = new Vector2(164, 30);
        }
    }

    private static RectTransform CreateRoot(Transform parent, GameUiStyle style)
    {
        var root = NativeUi.Rect("DungeonGuideControls", parent);
        root.anchorMin = root.anchorMax = root.pivot = new Vector2(0.5f, 1);
        root.anchoredPosition = new Vector2(0, -12);
        NativeUi.Panel(root, style);

        return root;
    }

    private static RectTransform CreateContainer(string name, Transform parent)
    {
        var container = NativeUi.Rect(name, parent);
        container.anchorMin = Vector2.zero;
        container.anchorMax = Vector2.one;
        container.sizeDelta = Vector2.zero;

        return container;
    }

    private (TMP_Text Floor, TMP_Text More) CreateFloorControls(Action<GuideCommand> command, GameUiStyle style)
    {
        Add(_first, _details, style, _text.FloorDown, 80, () => command(GuideCommand.FloorDown));
        var floorLabel = NativeUi.Label(_details, style, "", Vector2.zero, Vector2.zero);
        _first.Add((floorLabel.rectTransform, 250));
        Add(_first, _details, style, _text.FloorUp, 80, () => command(GuideCommand.FloorUp));
        Add(_first, _details, style, _text.FollowFloor, 130, () => command(GuideCommand.Follow));
        var moreLabel = Add(_first, _details, style, _text.More, 100, _state.Toggle);

        return (floorLabel, moreLabel);
    }

    private void CreateNavigationControls(Action<GuideCommand> command, GameUiStyle style)
    {
        Add(_second, _details, style, _text.NavigateUp, 140, () => command(GuideCommand.NavigateUp));
        Add(_second, _details, style, _text.NavigateDown, 140, () => command(GuideCommand.NavigateDown));
        Add(_second, _details, style, _text.Explore, 120, () => command(GuideCommand.Explore));
        Add(_second, _details, style, _text.Entrance, 110, () => command(GuideCommand.Entrance));
        Add(_second, _details, style, _text.Stop, 110, () => command(GuideCommand.Stop));
        Add(_second, _details, style, _text.Chests, 180, () => command(GuideCommand.NextChest));
    }

    private (TMP_Text Reveal, UnityEngine.UI.Button Rescan, TMP_Text Break, TMP_Text Jump, RectTransform Hints)
        CreateAdvancedControls(Action<GuideCommand> command, GameUiStyle style)
    {
        var revealLabel = Add(_extra, _advanced, style, _text.Explored, 210, () => command(GuideCommand.ToggleReveal));
        var rescan = Add(_extra, _advanced, style, _text.Rescan, 160, () => command(GuideCommand.Rescan));
        var rescanButton = rescan.transform.parent.GetComponent<UnityEngine.UI.Button>();
        var breakLabel = Add(_extra, _advanced, style, "", 260, () => command(GuideCommand.ToggleBreakables));
        var jumpLabel = Add(_extra, _advanced, style, "", 230, () => command(GuideCommand.ToggleJumps));
        var hints = NativeUi.Label(_advanced, style, _text.ChestControls, Vector2.zero, Vector2.zero, 14).rectTransform;

        return (revealLabel, rescanButton, breakLabel, jumpLabel, hints);
    }
}
