using Xunit;

namespace DungeonGuide.Tests;

/// <summary>
/// Prevents outdoor bases and stale dungeon sessions from replacing the ordinary world map.
/// </summary>
public sealed class DungeonActivationTests
{
    /// <summary>
    /// A rescan at an outdoor base must not create a session with default permissions.
    /// </summary>
    [Fact]
    public void OutdoorManualScanRequiresOptIn()
    {
        Assert.False(new DungeonActivationPolicy().CanScanManually(false));
    }

    /// <summary>
    /// Explicit outdoor permission enables manual mapping without changing interior behavior.
    /// </summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void AllowsRequestedManualScan(bool inInterior, bool allowOutdoorMapping)
    {
        Assert.True(new DungeonActivationPolicy().CanScanManually(inInterior, allowOutdoorMapping));
    }

    /// <summary>
    /// Teleporting outside an interior closes either session kind even within its former scan bounds.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LeavingInteriorRestoresWorldMapByDefault(bool manual)
    {
        Assert.False(new DungeonActivationPolicy().CanRetainSession(manual, true, false));
    }

    /// <summary>
    /// Outdoor permission retains only explicitly requested manual areas, never stale generated dungeons.
    /// </summary>
    [Theory]
    [InlineData(false, true, false, true, false)]
    [InlineData(true, true, false, true, true)]
    [InlineData(true, true, false, false, false)]
    [InlineData(true, false, false, true, false)]
    [InlineData(false, true, true, false, true)]
    [InlineData(true, true, true, false, true)]
    [InlineData(false, false, true, false, false)]
    public void RetainsOnlyEligibleSession(
        bool manual,
        bool insideBounds,
        bool inInterior,
        bool allowOutdoorMapping,
        bool expected)
    {
        Assert.Equal(expected, new DungeonActivationPolicy().CanRetainSession(
            manual,
            insideBounds,
            inInterior,
            allowOutdoorMapping));
    }
}
