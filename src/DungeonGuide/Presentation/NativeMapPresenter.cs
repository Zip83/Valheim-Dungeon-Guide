using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGuide;

/// <summary>
/// Coordinates the dungeon layers attached to the native large map and minimap.
/// </summary>
internal sealed class NativeMapPresenter : IDisposable
{
    private readonly GuideText _text;

    private readonly MapViewState _state;
    private readonly MapTextureRenderer _renderer;
    private readonly Action<GuideCommand> _command;
    private readonly Action<ChestRecord> _select;
    private readonly MapViewportController _viewport;
    private NativeMapLayer _small;
    private NativeMapLayer _large;
    private MapControlsView _controls;
    private OverviewGridView _overview;
    private readonly DungeonOverviewRenderer _overviewRenderer = new();
    private Minimap _boundMap;
    private bool _controlsCzech;
    private float _lastFloorHeight = float.NaN;
    private int _boundFloorCount;
    private readonly FloorNumbering _numbering;

    /// <summary>
    /// Whether a dungeon layer currently owns native map interaction.
    /// </summary>
    public bool DungeonVisible
    {
        get; private set;
    }

    /// <summary>
    /// Whether the native large map is currently open.
    /// </summary>
    public bool IsLargeOpen => _boundMap && Minimap.IsOpen();

    /// <summary>
    /// Connects native map layers, viewport interaction, floor labels, and command dispatch.
    /// </summary>
    public NativeMapPresenter(
        MapViewState state,
        MapTextureRenderer renderer,
        MapViewportController viewport,
        FloorNumbering numbering,
        Action<GuideCommand> command,
        Action<ChestRecord> select,
        GuideText text)
    {
        _text = text;
        _state = state;
        _renderer = renderer;
        _viewport = viewport;
        _numbering = numbering;
        _command = command;
        _select = select;
    }

    private void Bind()
    {
        if (_boundMap == Minimap.instance && _controls?.Root && _controlsCzech == _text.IsCzech)
        {
            return;
        }

        Unbind();
        _boundMap = Minimap.instance;
        if (!_boundMap)
        {
            return;
        }

        var bindings = new NativeMapBindings(_boundMap);
        var sm = bindings.SmallImage;
        var lg = bindings.LargeImage;
        var root = bindings.LargeRoot;
        if (!sm || !lg || !root)
        {
            throw new InvalidOperationException(_text.MapPanelsUnavailable);
        }

        var style = GameUiStyle.Read();
        _small = new NativeMapLayer(sm, _renderer.Texture, _renderer.Markers, bindings.Chrome(false));
        _large = new NativeMapLayer(lg, _renderer.Texture, _renderer.Markers, bindings.Chrome(true));

        _controls = new MapControlsView(root.transform, _numbering, _command, _text, style);
        _overview = new OverviewGridView(root.transform, style, _text, _numbering, OpenOverviewFloor);

        _controlsCzech = _text.IsCzech;
    }

    /// <summary>
    /// Requests a fresh fit of the selected floor.
    /// </summary>
    public void Center() => _viewport.Center();

    /// <summary>
    /// Attaches or updates dungeon layers and controls for the current native map state.
    /// </summary>
    public void Tick(DungeonSession session)
    {
        DungeonVisible = false;
        if (!Minimap.instance)
        {
            Unbind();
            return;
        }

        if (session != null)
        {
            Bind();
        }

        if (_controls == null)
        {
            return;
        }

        _controls.Refresh(session, _state, _renderer.DisplayedFloor, Minimap.IsOpen());
        var ready = session?.Scan != null && _renderer.DisplayedFloor >= 0 && session.Floors.Count > 0 && _state.Dungeon;
        DungeonVisible = ready;
        if (ready && Player.m_localPlayer)
        {
            ShowDungeon(session);
        }
        else
        {
            HideDungeon();
        }

        var overviewVisible = ready && Minimap.IsOpen() && _state.Overview;
        UpdateOverview(session, overviewVisible);
        if (ready && Minimap.IsOpen() && !overviewVisible)
        {
            _viewport.Tick(_large, _controls.Root, session, _state, _renderer, _select);
        }
    }

    private void OpenOverviewFloor(int floor)
    {
        if (!_overviewRenderer.CanSelect(floor))
        {
            return;
        }

        _state.OpenFloor(floor, _boundFloorCount);
        Center();
    }

    private void UpdateOverview(DungeonSession session, bool visible)
    {
        _overview.SetVisible(visible);
        if (session == null)
        {
            _overviewRenderer.Dispose();
            return;
        }

        if (!visible)
        {
            return;
        }

        _boundFloorCount = session.Floors.Count;
        _overviewRenderer.Tick(session, _state.RevealAll);
        _overview.Refresh(session, _overviewRenderer, _controls.Root.rect.height);
    }

    private void Unbind()
    {
        DungeonVisible = false;
        _small?.Dispose();
        _large?.Dispose();
        _small = _large = null;

        _overview?.Dispose();
        _overview = null;
        _overviewRenderer.Dispose();
        _boundFloorCount = 0;

        _controls?.Dispose();
        _controls = null;
        _boundMap = null;
    }

    /// <inheritdoc/>
    public void Dispose() => Unbind();

    private void ShowDungeon(DungeonSession session)
    {
        _small.SetSurface(_renderer.Texture, _renderer.Material);
        _large.SetSurface(_renderer.Texture, _renderer.Material);
        var uv = _renderer.UV(UnityPoints.P(Player.m_localPlayer.transform.position));
        var height = _renderer.DisplayedHeight;
        if (float.IsNaN(_lastFloorHeight) || Mathf.Abs(_lastFloorHeight - height) > 1.2f)
        {
            _viewport.Center();
        }

        _lastFloorHeight = height;

        _viewport.FollowOpening(() => _renderer.FitFloor(session, _renderer.DisplayedFloor), Minimap.IsOpen());
        var span = Mathf.Clamp(40 / Mathf.Max(1, _renderer.Span), 0.12f, 1);
        _small.Show(true, new Rect(uv.x - span / 2, uv.y - span / 2, span, span));
        _large.Show(true, _viewport.Viewport);
    }

    private void HideDungeon()
    {
        _small.Show(false, new Rect(0, 0, 1, 1));
        _large.Show(false, _viewport.Viewport);
    }
}
