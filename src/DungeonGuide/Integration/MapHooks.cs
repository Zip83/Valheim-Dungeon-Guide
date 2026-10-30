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
    // Exact member names form the reflection contract with the loaded Valheim version.
    private const string OnMapLeftDownMethod = "OnMapLeftDown";
    private const string OnMapLeftUpMethod = "OnMapLeftUp";
    private const string OnMapDblClickMethod = "OnMapDblClick";
    private const string OnMapLeftClickMethod = "OnMapLeftClick";
    private const string OnMapMiddleClickMethod = "OnMapMiddleClick";
    private const string OnSmallMapClickMethod = "OnSmallMapClick";
    private const string RemovePinUnderPointerMethod = "RemovePinUnderPointer";
    private const string DebugTeleportMethod = "DebugTeleport";
    private const string OnPressedPingIconMethod = "OnPressedPingIcon";
    private const string CreateMapNamePinMethod = "CreateMapNamePin";
    private const string ShowPinNameInputMethod = "ShowPinNameInput";
    private const string ShowPointOnMapMethod = "ShowPointOnMap";
    private const string SetMapModeMethod = "SetMapMode";

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

        InstallPointerGuards();
        InstallWorldReveal();
        InstallModeGuard();
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

    private void InstallPointerGuards()
    {
        string[] blocked =
        {
            OnMapLeftDownMethod,
            OnMapLeftUpMethod,
            OnMapDblClickMethod,
            OnMapLeftClickMethod,
            OnMapMiddleClickMethod,
            OnSmallMapClickMethod,
            RemovePinUnderPointerMethod,
            DebugTeleportMethod,
            OnPressedPingIconMethod,
            CreateMapNamePinMethod,
            ShowPinNameInputMethod
        };

        foreach (var method in typeof(Minimap).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => blocked.Contains(m.Name)))
        {
            _harmony.Patch(method, new HarmonyMethod(typeof(MapHooks), nameof(AllowPointer)));
        }
    }

    private void InstallWorldReveal()
    {
        foreach (var method in typeof(Minimap).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.Name == ShowPointOnMapMethod))
        {
            _harmony.Patch(method, prefix: new HarmonyMethod(typeof(MapHooks), nameof(RevealWorld)));
        }
    }

    private void InstallModeGuard()
    {
        foreach (var method in typeof(Minimap).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.Name == SetMapModeMethod))
        {
            _harmony.Patch(method, prefix: new HarmonyMethod(typeof(MapHooks), nameof(AllowModeChange)));
        }
    }
}
