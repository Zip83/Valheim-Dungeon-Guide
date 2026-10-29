using System;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Routes user commands to navigation, chest actions, and view state changes.
/// </summary>
internal sealed class CommandDispatcher
{
    private readonly GuideText _text;
    private readonly GuideRuntime _runtime;
    private readonly MapViewState _view;
    private readonly NavigationCoordinator _navigator;
    private readonly ChestActions _chests;
    private readonly GuideSettings _settings;
    private readonly UserNotice _notice;
    private readonly FloorNavigationPolicy _floorPolicy;

    /// <summary>
    /// Raised when map visibility or route display preferences change.
    /// </summary>
    public event Action ViewChanged;

    /// <summary>
    /// Raised when the selected floor should be fitted to the full map viewport.
    /// </summary>
    public event Action FitRequested;

    /// <summary>
    /// Connects user commands to their owning navigation, chest, and view services.
    /// </summary>
    public CommandDispatcher(
        GuideRuntime runtime,
        MapViewState view,
        NavigationCoordinator navigator,
        ChestActions chests,
        GuideSettings settings,
        UserNotice notice,
        FloorNavigationPolicy floorPolicy,
        GuideText text)
    {
        _text = text;
        _runtime = runtime;
        _view = view;
        _navigator = navigator;
        _chests = chests;
        _settings = settings;
        _notice = notice;
        _floorPolicy = floorPolicy;
    }

    /// <summary>
    /// Applies one user command to its owning service and reports relevant view changes.
    /// </summary>
    public void Execute(GuideCommand command)
    {
        if (command == GuideCommand.Rescan)
        {
            _runtime.Rescan();
            return;
        }

        if (command == GuideCommand.ToggleOverlay)
        {
            _view.Overlay = !_view.Overlay;
            return;
        }

        var session = _runtime.Session;
        if (session == null)
        {
            return;
        }

        switch (command)
        {
            case GuideCommand.ToggleWorld:
                _view.Dungeon = !_view.Dungeon;
                break;
            case GuideCommand.ToggleReveal:
                _view.RevealAll = !_view.RevealAll;
                _settings.SaveView(_view);
                break;
            case GuideCommand.ToggleBreakables:
                _view.AllowBreakables = !_view.AllowBreakables;
                _settings.SaveView(_view);
                break;
            case GuideCommand.ToggleJumps:
                _view.AllowJumps = !_view.AllowJumps;
                _settings.SaveView(_view);
                break;
            case GuideCommand.FloorUp:
                _view.ChangeFloor(1, session.Floors.Count);
                break;
            case GuideCommand.FloorDown:
                _view.ChangeFloor(-1, session.Floors.Count);
                break;
            case GuideCommand.Follow:
                _view.Follow();
                FitRequested?.Invoke();
                break;
            case GuideCommand.NavigateUp:
                NavigateFloor(session, 1);
                break;
            case GuideCommand.NavigateDown:
                NavigateFloor(session, -1);
                break;
            case GuideCommand.Explore:
                _navigator.Explore();
                break;
            case GuideCommand.Entrance:
                _navigator.Entrance();
                break;
            case GuideCommand.Stop:
                _navigator.Stop();
                break;
            case GuideCommand.MarkChest:
                _chests.Mark(session);
                break;
            case GuideCommand.NameChest:
                _chests.Name(session, () => _runtime.Session == session);
                break;
            case GuideCommand.CollectChest:
                var collected = _chests.Collect(session);
                if (collected != null && _navigator.Chest == collected)
                {
                    _navigator.Stop();
                }

                break;
            case GuideCommand.NextChest:
                _chests.Observe(session);
                var list = session.Chests.Targets();
                if (list.Count == 0)
                {
                    _notice.Show(_text.NoKnownChest);
                    break;
                }

                var index = list.FindIndex(c => c == _navigator.Chest);
                _navigator.ToChest(list[(index + 1) % list.Count]);
                break;
        }

        if (session.Scan != null && Player.m_localPlayer)
        {
            _navigator.Recalculate(session, _view, Player.m_localPlayer.transform.position);
        }

        ViewChanged?.Invoke();
    }

    private void NavigateFloor(DungeonSession session, int delta)
    {
        var floor = _floorPolicy.Next(session.Layout, Player.m_localPlayer.transform.position.y, delta);
        if (floor < 0 || floor >= session.Floors.Count)
        {
            _notice.Show(_text.NoAdjacentFloor);
            return;
        }

        _view.Follow();
        _navigator.Floor(floor, session.Floors[floor]);
    }
}
