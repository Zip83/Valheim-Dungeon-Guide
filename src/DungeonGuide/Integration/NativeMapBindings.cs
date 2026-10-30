using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace DungeonGuide;

/// <summary>Adapts private Valheim map fields without owning presentation or original game actions.</summary>
internal sealed class NativeMapBindings
{
    // Exact member names form the reflection contract with the loaded Valheim version.
    private const string MapImageSmallField = "m_mapImageSmall";
    private const string MapImageLargeField = "m_mapImageLarge";
    private const string LargeRootField = "m_largeRoot";
    private const string PinRootLargeField = "m_pinRootLarge";
    private const string PinNameRootLargeField = "m_pinNameRootLarge";
    private const string LargeMarkerField = "m_largeMarker";
    private const string LargeShipMarkerField = "m_largeShipMarker";
    private const string BiomeNameLargeField = "m_biomeNameLarge";
    private const string PinRootSmallField = "m_pinRootSmall";
    private const string PinNameRootSmallField = "m_pinNameRootSmall";
    private const string SmallMarkerField = "m_smallMarker";
    private const string SmallShipMarkerField = "m_smallShipMarker";
    private const string BiomeNameSmallField = "m_biomeNameSmall";

    private static readonly string[] s_largeChrome =
    {
        PinRootLargeField,
        PinNameRootLargeField,
        LargeMarkerField,
        LargeShipMarkerField,
        BiomeNameLargeField
    };

    private static readonly string[] s_smallChrome =
    {
        PinRootSmallField,
        PinNameRootSmallField,
        SmallMarkerField,
        SmallShipMarkerField,
        BiomeNameSmallField
    };

    private readonly Minimap _boundMap;

    /// <summary>Retains the currently loaded map whose UI fields will be inspected.</summary>
    /// <param name="map">Loaded native map instance.</param>
    public NativeMapBindings(Minimap map)
    {
        _boundMap = map;
    }

    /// <summary>Native small-map image, or null when unavailable on this game version.</summary>
    public RawImage SmallImage => Field(MapImageSmallField) as RawImage;

    /// <summary>Native large-map image, or null when unavailable on this game version.</summary>
    public RawImage LargeImage => Field(MapImageLargeField) as RawImage;

    /// <summary>Native large-map panel, or null when unavailable on this game version.</summary>
    public GameObject LargeRoot => Object(LargeRootField);

    private object Field(string name) => _boundMap.GetType()
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
