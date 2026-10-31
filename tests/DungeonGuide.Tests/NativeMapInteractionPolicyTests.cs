using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Prevents dungeon pointer handling from breaking native map lifecycle and other map plugins.</summary>
public sealed class NativeMapInteractionPolicyTests
{
    /// <summary>UpdatePins requires name UI creation even when native pointer interaction is blocked.</summary>
    [Theory]
    [InlineData("CreateMapNamePin")]
    [InlineData("UpdatePins")]
    [InlineData("DestroyPinMarker")]
    [InlineData("AddPin")]
    [InlineData("RemovePin")]
    [InlineData("Update")]
    public void PreservesNativeMarkerMaintenance(string method)
    {
        Assert.False(NativeMapInteractionPolicy.RequiresPointerGuard(method));
    }

    /// <summary>Clicks on the dungeon layer cannot accidentally edit or ping the underlying world map.</summary>
    [Theory]
    [InlineData("OnMapLeftDown")]
    [InlineData("OnMapLeftUp")]
    [InlineData("OnMapDblClick")]
    [InlineData("OnMapLeftClick")]
    [InlineData("OnMapMiddleClick")]
    [InlineData("OnSmallMapClick")]
    [InlineData("RemovePinUnderPointer")]
    [InlineData("DebugTeleport")]
    [InlineData("OnPressedPingIcon")]
    [InlineData("ShowPinNameInput")]
    public void GuardsOnlyPointerActions(string method)
    {
        Assert.True(NativeMapInteractionPolicy.RequiresPointerGuard(method));
    }
}
