using System;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGuide;

/// <summary>Owns one native-styled clickable floor thumbnail and its localized caption.</summary>
internal sealed class OverviewFloorCard : IDisposable
{
    private readonly RawImage _image;
    private readonly TMPro.TMP_Text _caption;
    private readonly RectTransform _root;

    /// <summary>Creates an independently handled floor button without inheriting native map actions.</summary>
    public OverviewFloorCard(Transform parent, GameUiStyle style, Action select)
    {
        _caption = NativeUi.Button(parent, style, "", select);
        _root = (RectTransform)_caption.transform.parent;

        _caption.rectTransform.anchorMin = new Vector2(0, 1);
        _caption.rectTransform.anchorMax = Vector2.one;
        _caption.rectTransform.pivot = new Vector2(0.5f, 1);
        _caption.rectTransform.anchoredPosition = new Vector2(0, -OverviewStyle.CaptionInset);
        _caption.rectTransform.sizeDelta = new Vector2(-OverviewStyle.Gap, OverviewStyle.CaptionHeight);

        var image = NativeUi.Rect("FloorThumbnail", _root);
        image.anchorMin = Vector2.zero;
        image.anchorMax = Vector2.one;
        image.offsetMin = new Vector2(OverviewStyle.CardInset, OverviewStyle.CardInset);
        image.offsetMax = new Vector2(-OverviewStyle.CardInset, -OverviewStyle.CardInset - OverviewStyle.CaptionHeight);

        _image = image.gameObject.AddComponent<RawImage>();
        _image.raycastTarget = false;
    }

    /// <summary>Updates the label and displays only a completed renderer-owned image.</summary>
    public void Refresh(string caption, Texture texture)
    {
        _caption.text = caption;

        _image.texture = texture;
        _image.enabled = texture;
    }

    /// <summary>Places the card in descending floor order after newly discovered floors are inserted.</summary>
    public void SetOrder(int order)
    {
        if (_root.GetSiblingIndex() != order)
        {
            _root.SetSiblingIndex(order);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_root)
        {
            _root.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(_root.gameObject);
        }
    }
}
