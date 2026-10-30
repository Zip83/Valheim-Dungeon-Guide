using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects explicitly expanded map controls and their collapse-on-open behaviour.</summary>
public sealed class MapToolbarTests
{
    /// <summary>Advanced controls stay closed until explicitly toggled.</summary>
    [Fact]
    public void StartsCollapsedAndPreservesExplicitExpansion()
    {
        var state = new MapToolbarState();
        state.SetMapOpen(true);
        Assert.False(state.Expanded);
        state.Toggle();
        state.SetMapOpen(true);
        Assert.True(state.Expanded);
    }

    /// <summary>Closing and reopening the map does not restore an old advanced panel.</summary>
    [Fact]
    public void ReopeningResetsAdvancedControls()
    {
        var state = new MapToolbarState();
        state.SetMapOpen(true);
        state.Toggle();
        state.SetMapOpen(false);
        state.SetMapOpen(true);

        Assert.False(state.Expanded);
    }

    /// <summary>Switching away from the dungeon closes advanced controls without opening them again.</summary>
    [Fact]
    public void ExplicitCollapseSurvivesRefresh()
    {
        var state = new MapToolbarState();
        state.SetMapOpen(true);
        state.Toggle();
        state.Collapse();
        state.SetMapOpen(true);

        Assert.False(state.Expanded);
    }
}
