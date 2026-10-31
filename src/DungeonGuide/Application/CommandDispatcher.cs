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
    private readonly WaypointActions _waypoints;
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
        WaypointActions waypoints,
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
        _waypoints = waypoints;
        _settings = settings;
        _notice = notice;
        _floorPolicy = floorPolicy;
    }

    /// <summary>
    /// Applies one user command to its owning service and reports relevant view changes.
    /// </summary>
    public void Execute(GuideCommand command)
    {
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

        ApplyCommand(command, session);

        if (session.Scan != null && Player.m_localPlayer)
        {
            _navigator.Recalculate(session, Player.m_localPlayer.transform.position);
        }

        ViewChanged?.Invoke();
    }

    private void NavigateFloor(DungeonSession session, int delta)
    {
        var floor = _floorPolicy.Next(session.Layout, Player.m_localPlayer.transform.position.y, delta);
        if (floor < 0 || floor >= session.Floors.Count)
        {
            _notice.Show(session.HasPartialGeometry && session.Scanning ? _text.PreparingRoute : _text.NoAdjacentFloor);
            return;
        }

        _view.Follow();
        _navigator.Floor(floor, session.Floors[floor], delta);
    }

    private void ApplyCommand(GuideCommand command, DungeonSession session)
    {
        if (_waypoints.TryExecute(command, session))
        {
            return;
        }

        if (!ApplyViewCommand(command, session) && !ApplyNavigationCommand(command, session))
        {
            ApplyChestCommand(command, session);
        }
    }

    private bool ApplyViewCommand(GuideCommand command, DungeonSession session)
    {
        switch (command)
        {
            case GuideCommand.ToggleWorld:
                _view.Dungeon = !_view.Dungeon;
                return true;
            case GuideCommand.ToggleOverview:
                _view.Overview = !_view.Overview;
                return true;
            case GuideCommand.ToggleReveal:
                _view.RevealAll = !_view.RevealAll;
                _settings.SaveView(_view);
                return true;
            case GuideCommand.FloorUp:
                _view.Overview = false;
                _view.ChangeFloor(1, session.Floors.Count);
                return true;
            case GuideCommand.FloorDown:
                _view.Overview = false;
                _view.ChangeFloor(-1, session.Floors.Count);
                return true;
            case GuideCommand.Follow:
                _view.Overview = false;
                _view.Follow();
                FitRequested?.Invoke();
                return true;
        }

        return false;
    }

    private bool ApplyNavigationCommand(GuideCommand command, DungeonSession session)
    {
        switch (command)
        {
            case GuideCommand.NavigateUp:
                NavigateFloor(session, 1);
                return true;
            case GuideCommand.NavigateDown:
                NavigateFloor(session, -1);
                return true;
            case GuideCommand.Explore:
                _navigator.Explore();
                return true;
            case GuideCommand.Entrance:
                _navigator.Entrance();
                return true;
            case GuideCommand.Stop:
                _navigator.Stop();
                return true;
        }

        return false;
    }

    private void ApplyChestCommand(GuideCommand command, DungeonSession session)
    {
        switch (command)
        {
            case GuideCommand.MarkChest:
                _chests.Mark(session);
                break;
            case GuideCommand.NameChest:
                _chests.Name(session, () => _runtime.Session == session);
                break;
            case GuideCommand.CollectChest:
                CollectChest(session);

                break;
            case GuideCommand.NextChest:
                SelectNextChest(session);
                break;
        }
    }

    private void CollectChest(DungeonSession session)
    {
        var collected = _chests.Collect(session);
        if (collected != null && _navigator.Chest == collected)
        {
            _navigator.Stop();
        }
    }

    private void SelectNextChest(DungeonSession session)
    {
        _chests.Observe(session);
        var list = session.Chests.Targets();
        if (list.Count == 0)
        {
            _notice.Show(_text.NoKnownChest);
            return;
        }

        var index = list.FindIndex(c => c == _navigator.Chest);
        _navigator.ToChest(list[(index + 1) % list.Count]);
    }
}
