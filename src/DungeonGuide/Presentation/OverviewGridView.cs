using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGuide;

/// <summary>Owns the responsive scrolling overview and incrementally creates native-styled floor cards.</summary>
internal sealed class OverviewGridView : IDisposable
{
    private readonly RectTransform _root;
    private readonly GridLayoutGroup _grid;
    private readonly TMP_Text _hint;
    private readonly GuideText _text;
    private readonly FloorNumbering _numbering;
    private readonly GameUiStyle _style;
    private readonly Action<int> _select;
    private readonly Dictionary<int, OverviewFloorCard> _cards = new();

    private int _revision = -1;

    /// <summary>Creates a clipped, vertically scrollable panel below the existing toolbar.</summary>
    public OverviewGridView(Transform parent, GameUiStyle style, GuideText text, FloorNumbering numbering,
        Action<int> select)
    {
        _text = text;
        _numbering = numbering;
        _style = style;
        _select = select;

        _root = NativeUi.Rect("DungeonOverview", parent);
        _root.anchorMin = Vector2.zero;
        _root.anchorMax = Vector2.one;
        NativeUi.Panel(_root, style);

        _hint = NativeUi.Label(_root, style, text.OverviewHint,
            new Vector2(OverviewStyle.Gap, -OverviewStyle.CardInset), Vector2.zero);
        _hint.rectTransform.anchorMax = Vector2.one;
        _hint.rectTransform.anchorMin = new Vector2(0, 1);
        _hint.rectTransform.sizeDelta = new Vector2(-OverviewStyle.Gap * 2, OverviewStyle.HintHeight);

        _grid = CreateScrollGrid(_root);
        _root.gameObject.SetActive(false);
    }

    /// <summary>Shows or hides the overview without changing minimap or single-floor rendering.</summary>
    public void SetVisible(bool visible) => _root.gameObject.SetActive(visible);

    /// <summary>Fits below the toolbar and refreshes cards without disclosing unlisted floor metadata.</summary>
    public void Refresh(DungeonSession session, DungeonOverviewRenderer renderer, float toolbarHeight)
    {
        _root.offsetMin = new Vector2(OverviewStyle.OuterMargin, OverviewStyle.OuterMargin);
        _root.offsetMax = new Vector2(-OverviewStyle.OuterMargin, -toolbarHeight - OverviewStyle.ToolbarGap);

        LayoutGrid();
        ReconcileCards(renderer);
        AddNextCard(renderer);
        RefreshCards(session, renderer);

        _hint.text = renderer.Floors.Count > 0 ? _text.OverviewHint
            : renderer.Preparing ? _text.PreparingMap : _text.NoExploredFloors;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var card in _cards.Values)
        {
            card.Dispose();
        }

        _cards.Clear();

        if (_root)
        {
            UnityEngine.Object.Destroy(_root.gameObject);
        }
    }

    private static GridLayoutGroup CreateScrollGrid(RectTransform parent)
    {
        var viewport = NativeUi.Rect("Viewport", parent);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(OverviewStyle.Gap, OverviewStyle.Gap);
        viewport.offsetMax = new Vector2(-OverviewStyle.Gap, -OverviewStyle.GridTop);
        viewport.gameObject.AddComponent<RectMask2D>();

        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.horizontal = false;
        scroll.scrollSensitivity = OverviewStyle.ScrollSpeed;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        var background = viewport.gameObject.AddComponent<Image>();
        background.color = MapPalette.MapBackground;

        var content = NativeUi.Rect("Floors", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1);
        content.sizeDelta = Vector2.zero;
        scroll.content = content;

        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.spacing = new Vector2(OverviewStyle.Gap, OverviewStyle.Gap);

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return grid;
    }

    private void LayoutGrid()
    {
        var width = Mathf.Max(1, _root.rect.width - OverviewStyle.Gap * 2);
        var columns = Mathf.Max(1, Mathf.FloorToInt(
            (width + OverviewStyle.Gap) / (OverviewStyle.MinimumCardWidth + OverviewStyle.Gap)));
        var side = Mathf.Max(1, (width - (columns - 1) * OverviewStyle.Gap) / columns);

        _grid.constraintCount = columns;
        _grid.cellSize = new Vector2(side, side + OverviewStyle.CaptionHeight);
    }

    private void ReconcileCards(DungeonOverviewRenderer renderer)
    {
        if (_revision == renderer.Revision)
        {
            return;
        }

        _revision = renderer.Revision;
        var visible = new HashSet<int>(renderer.Floors.Select(floor => floor.Floor));

        foreach (var floor in _cards.Keys.Where(floor => !visible.Contains(floor)).ToArray())
        {
            _cards[floor].Dispose();
            _cards.Remove(floor);
        }
    }

    private void AddNextCard(DungeonOverviewRenderer renderer)
    {
        foreach (var floor in renderer.Floors)
        {
            if (!_cards.ContainsKey(floor.Floor))
            {
                var selected = floor.Floor;
                _cards.Add(selected, new OverviewFloorCard(_grid.transform, _style, () => _select(selected)));
                break;
            }
        }
    }

    private void RefreshCards(DungeonSession session, DungeonOverviewRenderer renderer)
    {
        var order = 0;

        foreach (var floor in renderer.Floors)
        {
            if (_cards.TryGetValue(floor.Floor, out var card))
            {
                card.Refresh(_numbering.Label(session.Floors, floor.Floor, session.Area.Entrance?.y),
                    renderer.Texture(floor.Floor));
                card.SetOrder(order++);
            }
        }
    }
}
