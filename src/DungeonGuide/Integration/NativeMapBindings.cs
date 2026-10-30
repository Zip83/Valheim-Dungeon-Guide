using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGuide;

/// <summary>Adapts private Valheim map fields without owning presentation or original game actions.</summary>
internal sealed class NativeMapBindings
{
    private readonly Minimap _boundMap;

    /// <summary>Retains the currently loaded map whose UI fields will be inspected.</summary>
    /// <param name="map">Loaded native map instance.</param>
    public NativeMapBindings(Minimap map)
    {
        _boundMap = map;
    }

    /// <summary>Native small-map image, or null when unavailable on this game version.</summary>
    public RawImage SmallImage => Field("m_mapImageSmall") as RawImage;

    /// <summary>Native large-map image, or null when unavailable on this game version.</summary>
    public RawImage LargeImage => Field("m_mapImageLarge") as RawImage;

    /// <summary>Native large-map panel, or null when unavailable on this game version.</summary>
    public GameObject LargeRoot => Object("m_largeRoot");

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
    }; private object Field(string name) => _boundMap.GetType()
        .GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(_boundMap);

    private GameObject Object(string name)
    {
        var value = Field(name);
        return value is GameObject o ? o : value is Component c ? c.gameObject : null;
    }

    /// <summary>Enumerates native map decorations hidden while a dungeon layer is active.</summary>
    /// <param name="big">True for large-map decorations, false for minimap decorations.</param>
    /// <returns>Native decoration objects, including null for unavailable fields.</returns>
    public IEnumerable<GameObject> Chrome(bool big)
    {
        foreach (var field in big ? s_largeChrome : s_smallChrome)
        {
            yield return Object(field);
        }
    }
}
