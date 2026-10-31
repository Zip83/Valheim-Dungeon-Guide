using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Separates pointer actions from map maintenance that must run beneath the dungeon layer.</summary>
internal static class NativeMapInteractionPolicy
{
    // Exact names are the reflection contract; internal marker/name creation is deliberately absent.
    private static readonly HashSet<string> s_pointerMethods = new()
    {
        "OnMapLeftDown", "OnMapLeftUp", "OnMapDblClick", "OnMapLeftClick", "OnMapMiddleClick",
        "OnSmallMapClick", "RemovePinUnderPointer", "DebugTeleport", "OnPressedPingIcon", "ShowPinNameInput"
    };

    /// <summary>Guards only actual interaction entry points, never lifecycle or marker creation/update methods.</summary>
    public static bool RequiresPointerGuard(string method) => s_pointerMethods.Contains(method);
}
