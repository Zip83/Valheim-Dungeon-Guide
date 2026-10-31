using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGuide;

/// <summary>Owns a non-interactive native-styled overlay created only after explicit user activation.</summary>
internal sealed class OverlayPresenter : IDisposable
{
    private readonly GuideText _text;
    private readonly MapTextureRenderer _renderer;
    private readonly FloorNumbering _numbering;
    private RectTransform _root;
    private TMP_Text _title;
    private TMP_Text _footer;
    private RawImage _floor;
    private RawImage _markers;
    private Minimap _boundMap;
    private bool _czech;

    /// <summary>Shares the published floor/fog/marker layers with the native map without owning their textures.</summary>
    public OverlayPresenter(MapTextureRenderer renderer, FloorNumbering numbering, GuideText text)
    {
        _renderer = renderer;
        _numbering = numbering;
        _text = text;
    }

    private void Bind()
    {
        if (_root && _boundMap == Minimap.instance && _czech == _text.IsCzech)
        {
            return;
        }

        Dispose();
        var style = GameUiStyle.Read();
        _boundMap = Minimap.instance;
        _czech = _text.IsCzech;
        CreateFrame(style);
        CreateMapLayers();
        CreateFooter(style);
    }

    private void CreateFrame(GameUiStyle style)
    {
        _root = NativeUi.Rect("DungeonGuideOverlay", GameUiStyle.MapCanvas());
        _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(0, 1);
        _root.anchoredPosition = new Vector2(20, -90);
        _root.sizeDelta = new Vector2(470, 553);
        _root.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
        NativeUi.Panel(_root, style, false);
        _title = NativeUi.Label(_root, style, "", new Vector2(12, -8), new Vector2(446, 30));
    }

    private void CreateFooter(GameUiStyle style)
    {
        _footer = NativeUi.Label(_root, style, "", new Vector2(12, -495), new Vector2(446, 24), 14);
        NativeUi.Label(_root, style, _text.LegendOverlayHint, new Vector2(12, -520), new Vector2(446, 24), 14);
    }

    /// <summary>Updates the optional panel after game UI logic, without consuming pointer or keyboard input.</summary>
    public void Tick(DungeonSession session, MapViewState state)
    {
        if (!state.Overlay || session == null || !state.Dungeon || Minimap.IsOpen() || !Minimap.instance)
        {
            if (_root)
            {
                _root.gameObject.SetActive(false);
            }

            return;
        }

        Bind();
        _root.gameObject.SetActive(true);
        FitToCanvas();
        RefreshLabels(session, state);

        var ready = session.Scan != null && _renderer.DisplayedFloor >= 0;
        _floor.gameObject.SetActive(ready);
        if (ready)
        {
            _floor.texture = _renderer.Texture;
            _floor.uvRect = _markers.uvRect = _renderer.FitFloor(session, _renderer.DisplayedFloor);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_root)
        {
            UnityEngine.Object.Destroy(_root.gameObject);
        }

        _root = null;
        _boundMap = null;
    }

    private void CreateMapLayers()
    {
        var rect = NativeUi.Rect("FloorPlan", _root);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(12, -45);
        rect.sizeDelta = new Vector2(446, 446);
        _floor = rect.gameObject.AddComponent<RawImage>();
        _floor.texture = _renderer.Texture;
        _floor.raycastTarget = false;
        var marker = NativeUi.Rect("Markers", rect);
        marker.anchorMin = Vector2.zero;
        marker.anchorMax = Vector2.one;
        marker.sizeDelta = Vector2.zero;
        _markers = marker.gameObject.AddComponent<RawImage>();
        _markers.texture = _renderer.Markers;
        _markers.raycastTarget = false;
    }

    private void FitToCanvas()
    {
        var parent = (RectTransform)_root.parent;
        var scale = Mathf.Min(1, Mathf.Min(parent.rect.height / 800f, parent.rect.width / 520f));
        _root.localScale = new Vector3(scale, scale, 1);
    }

    private void RefreshLabels(DungeonSession session, MapViewState state)
    {
        _title.text = _renderer.DisplayedFloor < 0 || _renderer.DisplayedFloor != state.SelectedFloor ? _text.PreparingMap
            : _numbering.Label(session.Floors, _renderer.DisplayedFloor, session.Area.Entrance?.y)
                + (session.HasPartialGeometry ? _text.PreviewSuffix : "")
                + _text.Separator + (state.RevealAll ? _text.FullDungeon : _text.Explored);
        _footer.text = _text.OverlayControls;
    }
}
