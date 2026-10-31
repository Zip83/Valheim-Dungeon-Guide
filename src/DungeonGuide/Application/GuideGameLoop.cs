using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Schedules runtime tracking, navigation, rendering, and presentation without owning their implementations.
/// </summary>
internal sealed class GuideGameLoop : IDisposable
{
    private readonly GuideRuntime _runtime;
    private readonly MapViewState _view;
    private readonly InputRouter _input;
    private readonly ChestActions _chests;
    private readonly NavigationCoordinator _navigator;
    private readonly MapTextureRenderer _texture;
    private readonly NativeMapPresenter _maps;
    private readonly OverlayPresenter _overlay;
    private readonly NavigationHud _hud;
    private float _nextObserve;
    private float _nextRoute;
    private float _nextRender;
    private bool _disposed;
    private readonly DynamicRouteValidator _validator;
    private string _blockedHint;
    private IReadOnlyList<float> _knownFloors = Array.Empty<float>();

    /// <summary>
    /// Connects independently owned input, session, navigation, map, overlay, and instruction services.
    /// </summary>
    public GuideGameLoop(
        GuideRuntime runtime,
        MapViewState view,
        InputRouter input,
        ChestActions chests,
        NavigationCoordinator navigator,
        MapTextureRenderer texture,
        NativeMapPresenter maps,
        OverlayPresenter overlay,
        NavigationHud hud,
        DynamicRouteValidator validator)
    {
        _runtime = runtime;
        _view = view;
        _input = input;
        _chests = chests;
        _navigator = navigator;
        _texture = texture;
        _maps = maps;
        _overlay = overlay;
        _hud = hud;
        _validator = validator;
        runtime.Entered += Enter;
        runtime.Left += navigator.Stop;
        runtime.ScanCompleted += Scanned;
    }

    private void Enter()
    {
        _knownFloors = Array.Empty<float>();
        _view.Enter();
        _navigator.Stop();
        _maps.Center();
        _nextObserve = _nextRender = _nextRoute = 0;
    }

    private void Scanned()
    {
        var floors = _runtime.Session.Floors;
        _view.RemapFloors(_knownFloors, floors);
        _knownFloors = floors;
        _navigator.InvalidateRoute();
        _nextRender = _nextObserve = _nextRoute = 0;
    }

    /// <summary>
    /// Invalidates map presentation after a display or route preference changes.
    /// </summary>
    public void RefreshView()
    {
        _nextRender = _nextRoute = 0;
    }

    /// <summary>
    /// Advances bounded session tracking, input, chest observation, route updates, and texture rendering.
    /// </summary>
    public void Tick()
    {
        if (_disposed)
        {
            return;
        }

        _runtime.Tick();
        _input.Tick(_view.Dungeon && (_maps.IsLargeOpen || _view.Overlay));
        var session = _runtime.Session;
        _texture.Tick(session, _view);
        var player = Player.m_localPlayer;
        if (session == null || !player || session.Scan == null)
        {
            return;
        }

        _view.Track(session.Floors, player.transform.position.y);

        ObserveChests(session);
        UpdateRoute(session, player);
        RenderMap(session, player);
    }

    /// <summary>
    /// Updates native map attachments after ordinary game-frame logic.
    /// </summary>
    public void LateTick()
    {
        if (!_disposed)
        {
            _maps.Tick(_runtime.Session);
            _overlay.Tick(_runtime.Session, _view);
        }
    }

    /// <summary>
    /// Draws the manually enabled overlay and active navigation HUD.
    /// </summary>
    public void Draw()
    {
        if (!_disposed)
        {
            _hud.Draw(_runtime.Session, _view, _navigator, _blockedHint);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _runtime.Dispose();
        _runtime.Entered -= Enter;
        _runtime.Left -= _navigator.Stop;
        _runtime.ScanCompleted -= Scanned;
        _maps.Dispose();
        _overlay.Dispose();
        _texture.Dispose();
    }

    private void ObserveChests(DungeonSession session)
    {
        if (Time.unscaledTime >= _nextObserve)
        {
            _nextObserve = Time.unscaledTime + 3;
            _chests.Observe(session);
        }
    }

    private void UpdateRoute(DungeonSession session, Player player)
    {
        if (Time.unscaledTime >= _nextRoute)
        {
            _nextRoute = Time.unscaledTime + 1;
            _blockedHint = session.NavigationReady ? _validator.Inspect(session, _navigator, player) : null;
            _navigator.Recalculate(session, _view, player.transform.position);
        }
    }

    private void RenderMap(DungeonSession session, Player player)
    {
        if (Time.unscaledTime >= _nextRender)
        {
            _nextRender = Time.unscaledTime + 0.4f;
            if (_view.Dungeon)
            {
                _texture.Render(session, _view, _navigator, player);
            }
        }
    }
}
