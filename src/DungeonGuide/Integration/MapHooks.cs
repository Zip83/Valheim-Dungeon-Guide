using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace DungeonGuide;

/// <summary>
/// Installs narrow Harmony guards that preserve native world-map behavior outside dungeon interaction.
/// </summary>
internal sealed class MapHooks : IDisposable
{
    private readonly Harmony _harmony = new("zip.dungeonguide.maps");
    // Narrow callback bridge, installed and cleared with the plugin lifecycle.
    private static Func<bool> s_dungeonLayer;
    private static Action s_showWorld;
    private static Func<bool> s_modifiedMapKey;

    /// <summary>
    /// Installs native map interaction guards with callbacks whose lifetime is owned by this instance.
    /// </summary>
    public MapHooks(Func<bool> active, Action reveal, Func<bool> mapKey)
    {
        s_dungeonLayer = active;
        s_showWorld = reveal;
        s_modifiedMapKey = mapKey;
        string[] blocked =
        {
            "OnMapLeftDown",
            "OnMapLeftUp",
            "OnMapDblClick",
            "OnMapLeftClick",
            "OnMapMiddleClick",
            "OnSmallMapClick",
            "RemovePinUnderPointer",
            "DebugTeleport",
            "OnPressedPingIcon",
            "CreateMapNamePin",
            "ShowPinNameInput"
        };
        foreach (var method in typeof(Minimap).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => blocked.Contains(m.Name)))
        {
            _harmony.Patch(method, new HarmonyMethod(typeof(MapHooks), nameof(AllowPointer)));
        }

        foreach (var method in typeof(Minimap).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.Name == "ShowPointOnMap"))
        {
            _harmony.Patch(method, prefix: new HarmonyMethod(typeof(MapHooks), nameof(RevealWorld)));
        }

        foreach (var method in typeof(Minimap).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.Name == "SetMapMode"))
        {
            _harmony.Patch(method, prefix: new HarmonyMethod(typeof(MapHooks), nameof(AllowModeChange)));
        }
    }

    private static bool AllowPointer() => s_dungeonLayer == null || !s_dungeonLayer();

    private static void RevealWorld() => s_showWorld?.Invoke();

    private static bool AllowModeChange() => s_modifiedMapKey == null || !s_modifiedMapKey();

    /// <inheritdoc/>
    public void Dispose()
    {
        _harmony.UnpatchSelf();
        s_dungeonLayer = null;
        s_showWorld = null;
        s_modifiedMapKey = null;
    }
}
