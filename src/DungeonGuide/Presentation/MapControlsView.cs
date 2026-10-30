using System;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGuide;

/// <summary>
/// Owns the native full-map controls and updates their labels from the active session.
/// </summary>
internal sealed class MapControlsView : IDisposable
{
    private readonly GuideText _text;
    /// <summary>
    /// Root transform owning the full-map controls and their input area.
    /// </summary>
    public RectTransform Root
    {
        get;
    }

    private readonly RectTransform _details;
    private readonly Text _floorLabel;
    private readonly Text _revealLabel;
    private readonly Text _worldLabel;
    private readonly Text _breakLabel;
    private readonly Text _jumpLabel;
    private readonly FloorNumbering _numbering;

    /// <summary>
    /// Creates native map controls that dispatch user commands and display floor labels.
    /// </summary>
    public MapControlsView(Transform parent, FloorNumbering numbering, Action<GuideCommand> command, GuideText text)
    {
        _text = text;
        _numbering = numbering;
        Root = NativeUi.Rect("DungeonGuideControls", parent);
        Root.anchorMin = Root.anchorMax = Root.pivot = new Vector2(0.5f, 1);
        Root.anchoredPosition = new Vector2(0, -12);
        Root.sizeDelta = new Vector2(960, 110);
        Root.gameObject.AddComponent<Image>().color = MapPalette.Panel;
        _worldLabel = NativeUi.Button(Root, _text.WorldMap, 8, -7, 150, () => command(GuideCommand.ToggleWorld));
        _details = NativeUi.Rect("DungeonControls", Root);
        _details.anchorMin = Vector2.zero;
        _details.anchorMax = Vector2.one;
        _details.sizeDelta = Vector2.zero;
        _floorLabel = NativeUi.Label(_details, "", new Vector2(170, -7), new Vector2(260, 28));
        NativeUi.Button(_details, _text.FloorDown, 438, -7, 90, () => command(GuideCommand.FloorDown));
        NativeUi.Button(_details, _text.FloorUp, 534, -7, 90, () => command(GuideCommand.FloorUp));
        NativeUi.Button(_details, _text.FollowFloor, 630, -7, 115, () => command(GuideCommand.Follow));
        _revealLabel = NativeUi.Button(
            _details,
            _text.Explored,
            751,
            -7,
            200,
            () => command(GuideCommand.ToggleReveal));
        NativeUi.Button(_details, _text.NavigateUp, 8, -42, 125, () => command(GuideCommand.NavigateUp));
        NativeUi.Button(
            _details,
            _text.NavigateDown,
            139,
            -42,
            125,
            () => command(GuideCommand.NavigateDown));
        NativeUi.Button(_details, _text.Explore, 270, -42, 110, () => command(GuideCommand.Explore));
        NativeUi.Button(_details, _text.Entrance, 386, -42, 95, () => command(GuideCommand.Entrance));
        NativeUi.Button(_details, _text.Stop, 487, -42, 110, () => command(GuideCommand.Stop));
        NativeUi.Button(_details, _text.Rescan, 603, -42, 145, () => command(GuideCommand.Rescan));
        NativeUi.Button(_details, _text.Chests, 754, -42, 197, () => command(GuideCommand.NextChest));
        _breakLabel = NativeUi.Button(_details, "", 8, -77, 240, () => command(GuideCommand.ToggleBreakables));
        _jumpLabel = NativeUi.Button(_details, "", 254, -77, 240, () => command(GuideCommand.ToggleJumps));
        NativeUi.Label(_details, _text.ChestControls, new Vector2(500, -77), new Vector2(450, 28), 14);
    }

    /// <summary>
    /// Updates visibility and labels from the active floor, map mode, and route preferences.
    /// </summary>
    public void Refresh(DungeonSession session, MapViewState state)
    {
        if (!Root)
        {
            return;
        }

        Root.gameObject.SetActive(session != null);
        if (session == null)
        {
            return;
        }

        _worldLabel.text = state.Dungeon ? _text.WorldMap : _text.DungeonMap;
        _details.gameObject.SetActive(state.Dungeon);
        _floorLabel.text = session.Scan == null
            ? _text.PreparingMap
            : _numbering.Label(session.Floors, state.SelectedFloor, session.Area.Entrance?.y);
        _revealLabel.text = state.RevealAll ? _text.FullDungeon : _text.Explored;
        _breakLabel.text = _text.BreakablePassages + (state.AllowBreakables ? _text.Yes : _text.No);
        _jumpLabel.text = _text.VerifiedJumps + (state.AllowJumps ? _text.Yes : _text.No);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (Root)
        {
            UnityEngine.Object.Destroy(Root.gameObject);
        }
    }
}
