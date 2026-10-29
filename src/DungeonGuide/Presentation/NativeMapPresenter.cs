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
    private static readonly string[] s_largeChrome =
    {
        "m_pinRootLarge",
        "m_pinNameRootLarge",
        "m_largeMarker",
        "m_largeShipMarker",
        "m_biomeNameLarge"
    };

    private static readonly string[] s_smallChrome =
    {
        "m_pinRootSmall",
        "m_pinNameRootSmall",
        "m_smallMarker",
        "m_smallShipMarker",
        "m_biomeNameSmall"
    };

    private readonly MapViewState _state;
    private readonly MapTextureRenderer _renderer;
    private readonly Action<GuideCommand> _command;
    private readonly Action<ChestRecord> _select;
    private readonly MapViewportController _viewport;
    private NativeMapLayer _small;
    private NativeMapLayer _large;
    private MapControlsView _controls;
    private Minimap _boundMap;
    private int _lastFloor = -1;
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
        Action<ChestRecord> select)
    {
        _state = state;
        _renderer = renderer;
        _viewport = viewport;
        _numbering = numbering;
        _command = command;
        _select = select;
    }

    private object Field(string name) => _boundMap.GetType()
        .GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(_boundMap);

    private GameObject Object(string name)
    {
        var value = Field(name);
        return value is GameObject o ? o : value is Component c ? c.gameObject : null;
    }

    private IEnumerable<GameObject> Chrome(bool big)
    {
        foreach (var field in big ? s_largeChrome : s_smallChrome)
        {
            yield return Object(field);
        }
    }

    private void Bind()
    {
        if (_boundMap == Minimap.instance && _controls?.Root)
        {
            return;
        }

        Unbind();
        _boundMap = Minimap.instance;
        if (!_boundMap)
        {
            return;
        }

        var sm = Field("m_mapImageSmall") as RawImage;
        var lg = Field("m_mapImageLarge") as RawImage;
        var root = Object("m_largeRoot");
        if (!sm || !lg || !root)
        {
            throw new InvalidOperationException(GuideText.MapPanelsUnavailable);
        }

        _small = new NativeMapLayer(sm, _renderer.Texture, Chrome(false));
        _large = new NativeMapLayer(lg, _renderer.Texture, Chrome(true));
        _controls = new MapControlsView(root.transform, _numbering, _command);
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

        _controls.Refresh(session, _state);
        var ready = session?.Scan != null && session.Floors.Count > 0 && _state.Dungeon;
        DungeonVisible = ready;
        if (ready && Player.m_localPlayer)
        {
            var uv = _renderer.UV(UnityPoints.P(Player.m_localPlayer.transform.position));
            if (_lastFloor != _state.SelectedFloor)
            {
                _lastFloor = _state.SelectedFloor;
                _viewport.Center();
            }

            _viewport.FollowOpening(() => _renderer.FitFloor(session, _state.SelectedFloor), Minimap.IsOpen());
            var span = Mathf.Clamp(40 / Mathf.Max(1, _renderer.Span), 0.12f, 1);
            _small.Show(true, new Rect(uv.x - span / 2, uv.y - span / 2, span, span));
            _large.Show(true, _viewport.Viewport);
        }
        else
        {
            _small.Show(false, new Rect(0, 0, 1, 1));
            _large.Show(false, _viewport.Viewport);
        }

        if (ready && Minimap.IsOpen())
        {
            _viewport.Tick(_large, _controls.Root, session, _state, _renderer, _select);
        }
    }

    private void Unbind()
    {
        DungeonVisible = false;
        _small?.Dispose();
        _large?.Dispose();
        _small = _large = null;
        _controls?.Dispose();
        _controls = null;
        _boundMap = null;
    }

    /// <inheritdoc/>
    public void Dispose() => Unbind();
}
