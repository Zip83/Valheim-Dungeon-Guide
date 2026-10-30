using System;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Owns map opening fit, zoom, pan, and pointer selection independently of map rendering.
/// </summary>
internal sealed class MapViewportController
{
    /// <summary>
    /// Current normalized crop of the shared map texture.
    /// </summary>
    public Rect Viewport { get; private set; } = new Rect(0, 0, 1, 1);

    private Vector2 _lastMouse;
    private Vector2 _dragOrigin;
    private bool _dragging;
    private bool _centerPending = true;

    /// <summary>
    /// Requests fitting the current selected floor on the next presentation update.
    /// </summary>
    public void Center()
    {
        _centerPending = true;
        _dragging = false;
    }

    /// <summary>
    /// Fits the first opening while retaining later manual pan and zoom.
    /// </summary>
    public void FollowOpening(Func<Rect> fitFloor, bool open)
    {
        if (open && _centerPending)
        {
            Viewport = fitFloor();
            _centerPending = false;
        }
    }

    /// <summary>
    /// Handles dungeon-map pan, zoom, and chest selection without consuming control-panel input.
    /// </summary>
    public void Tick(
        NativeMapLayer layer,
        RectTransform controls,
        DungeonSession session,
        MapViewState state,
        MapTextureRenderer renderer,
        Action<ChestRecord> select)
    {
        Vector2 mouse = Input.mousePosition;
        if (RectTransformUtility.RectangleContainsScreenPoint(controls, mouse, null) || !layer.Contains(mouse))
        {
            _dragging = false;
            return;
        }

        var viewport = Zoom(layer, mouse);
        Drag(layer, mouse, ref viewport);
        FinishDrag(layer, session, state, renderer, select, mouse, viewport.width);
        viewport.x = Mathf.Clamp(viewport.x, -0.1f, 1.1f - viewport.width);
        viewport.y = Mathf.Clamp(viewport.y, -0.1f, 1.1f - viewport.height);
        Viewport = viewport;
    }

    private Rect Zoom(NativeMapLayer layer, Vector2 mouse)
    {
        var viewport = Viewport;
        var wheel = Input.mouseScrollDelta.y;
        if (Mathf.Abs(wheel) > 0.01f)
        {
            var uv = layer.MouseUV(mouse);
            var size = Mathf.Clamp(viewport.width * (wheel > 0 ? 0.83f : 1.2f), 0.12f, 1);
            viewport = new Rect(uv.x - size / 2, uv.y - size / 2, size, size);
        }

        return viewport;
    }

    private void Drag(NativeMapLayer layer, Vector2 mouse, ref Rect viewport)
    {
        if (Input.GetMouseButtonDown(0))
        {
            _dragging = true;
            _lastMouse = _dragOrigin = mouse;
        }

        if (_dragging && Input.GetMouseButton(0))
        {
            var delta = mouse - _lastMouse;
            var rect = layer.ImageRect.rect;
            viewport.position -= new Vector2(delta.x / Mathf.Max(1, rect.width), delta.y / Mathf.Max(1, rect.height))
                * viewport.width;
            _lastMouse = mouse;
        }
    }

    private void FinishDrag(
        NativeMapLayer layer,
        DungeonSession session,
        MapViewState state,
        MapTextureRenderer renderer,
        Action<ChestRecord> select,
        Vector2 mouse,
        float width)
    {
        if (_dragging && Input.GetMouseButtonUp(0))
        {
            _dragging = false;
            if (Vector2.Distance(_dragOrigin, mouse) < 6)
            {
                SelectChest(layer, session, state, renderer, select, mouse, width);
            }
        }
    }

    private static void SelectChest(
        NativeMapLayer layer,
        DungeonSession session,
        MapViewState state,
        MapTextureRenderer renderer,
        Action<ChestRecord> select,
        Vector2 mouse,
        float width)
    {
        var uv = layer.MouseUV(mouse);
        var chest = session.Data.chests.Where(c => !c.done && (state.RevealAll || c.discovered || c.collect)
                && FloorDetector.Nearest(session.Floors, c.position.y) == state.SelectedFloor)
            .OrderBy(c => Vector2.Distance(renderer.UV(c.position), uv))
            .FirstOrDefault();
        if (chest != null && Vector2.Distance(renderer.UV(chest.position), uv) < width * 0.025f)
        {
            select(chest);
        }
    }
}
