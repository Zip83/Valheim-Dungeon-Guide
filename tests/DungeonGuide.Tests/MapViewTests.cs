using System.Collections.Generic;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>
/// Protects floor selection, entrance-relative labels, and map extent fitting.
/// </summary>
public sealed class MapViewTests
{
    /// <summary>
    /// Czech floor labels describe their position relative to the known entrance.
    /// </summary>
    [Theory]
    [InlineData(1, 12f, "Vstupní patro 0")]
    [InlineData(0, 12f, "Patro −1")]
    [InlineData(2, 12f, "Patro +1")]
    [InlineData(0, null, "Patro 0 (odspodu)")]
    public void LabelsFloorRelativeToEntrance(int floor, float? entrance, string expected)
    {
        Assert.Equal(expected, new FloorNumbering(() => "Czech").Label(new List<float> { 0, 12, 24 }, floor, entrance));
    }

    /// <summary>
    /// Fitting includes every point on the selected floor without including another floor.
    /// </summary>
    [Fact]
    public void FitsOnlySelectedFloor()
    {
        var graph = ExtentGraph();
        var fit = new MapExtentFitter().Fit(graph, 0, new Point(0, 0, 0), 100);
        Assert.True(fit.X <= 0.8f && fit.X + fit.Size >= 0.9f && fit.Y <= 0.3f && fit.Y + fit.Size >= 0.4f);
        Assert.True(fit.Size < 0.2f);
    }

    /// <summary>
    /// An absent floor falls back to the full available map extent.
    /// </summary>
    [Fact]
    public void MissingFloorUsesFullViewport()
    {
        var fit = new MapExtentFitter().Fit(ExtentGraph(), 99, new Point(0, 0, 0), 100);
        Assert.Equal((0f, 0f, 1f), (fit.X, fit.Y, fit.Size));
    }

    /// <summary>
    /// The initial view shows explored geometry without opening an overlay.
    /// </summary>
    [Fact]
    public void StartsWithExploredOnlyAndHiddenOverlay()
    {
        var state = new MapViewState();
        Assert.False(state.RevealAll);
        Assert.False(state.Overlay);
    }

    /// <summary>
    /// Automatic tracking follows the floor while avoiding oscillation at stair midpoints.
    /// </summary>
    [Fact]
    public void TracksCurrentFloorWithHysteresis()
    {
        var state = new MapViewState();
        state.Track(Levels(), 6);
        Assert.Equal(1, state.CurrentFloor);
        state.Track(Levels(), 8.9f);
        Assert.Equal(1, state.CurrentFloor);
    }

    /// <summary>
    /// Manual browsing and world-map toggles preserve the actual player floor.
    /// </summary>
    [Fact]
    public void PreservesManualSelectionAcrossWorldMapToggle()
    {
        var state = new MapViewState();
        state.Track(Levels(), 6);
        state.ChangeFloor(1, 3);
        state.Dungeon = false;
        state.Dungeon = true;
        Assert.Equal(1, state.CurrentFloor);
        Assert.Equal(2, state.SelectedFloor);
    }

    /// <summary>
    /// Following restores the tracked floor after browsing another floor.
    /// </summary>
    [Fact]
    public void FollowRestoresCurrentFloor()
    {
        var state = new MapViewState();
        state.Track(Levels(), 6);
        state.ChangeFloor(1, 3);
        state.Follow();
        Assert.Equal(1, state.SelectedFloor);
    }

    /// <summary>
    /// Manual selection remains valid at both limits of the available floor list.
    /// </summary>
    [Theory]
    [InlineData(-100, 0)]
    [InlineData(100, 2)]
    public void ClampsManualSelection(int delta, int expected)
    {
        var state = new MapViewState();
        state.Track(Levels(), 6);
        state.ChangeFloor(delta, 3);
        Assert.Equal(expected, state.SelectedFloor);
    }

    /// <summary>
    /// Entering another dungeon resets browsing without enabling an overlay.
    /// </summary>
    [Fact]
    public void EnterResetsSelectionAndMapMode()
    {
        var state = new MapViewState();
        state.ChangeFloor(2, 3);
        state.Dungeon = false;
        state.Enter();
        Assert.True(state.Dungeon);
        Assert.Equal(0, state.SelectedFloor);
        Assert.False(state.Overlay);
    }

    /// <summary>
    /// A rescan with fewer floors must clamp a previous manual selection.
    /// </summary>
    [Fact]
    public void RescanClampsSelection()
    {
        var state = new MapViewState();
        state.ChangeFloor(2, 3);
        state.Track(new List<float> { 0 }, 0);
        Assert.Equal(0, state.SelectedFloor);
    }

    /// <summary>Adding lower floors must preserve the currently tracked physical elevation.</summary>
    [Fact]
    public void RemapsCurrentFloorByElevation()
    {
        var state = new MapViewState();
        var preview = new[] { 100f, 106f };
        state.Track(preview, 106);

        state.RemapFloors(preview, new[] { 88f, 94f, 100f, 106f, 112f });

        Assert.Equal(3, state.CurrentFloor);
        Assert.Equal(3, state.SelectedFloor);
    }

    /// <summary>Manual browsing remains independent from the player when replacement indices change.</summary>
    [Fact]
    public void RemapsBrowsedFloorSeparatelyFromPlayer()
    {
        var state = new MapViewState();
        var preview = new[] { 100f, 106f };
        state.ChangeFloor(1, preview.Length);

        state.RemapFloors(preview, new[] { 88f, 94f, 100f, 106f, 112f });

        Assert.Equal(2, state.CurrentFloor);
        Assert.Equal(3, state.SelectedFloor);
        state.Follow();
        Assert.Equal(2, state.SelectedFloor);
    }

    /// <summary>A refined elevation estimate selects the nearest replacement floor.</summary>
    [Fact]
    public void RemapsToRefinedFloorElevation()
    {
        var state = new MapViewState();
        state.RemapFloors(new[] { 100f }, new[] { 94f, 100.4f, 106f });

        Assert.Equal(1, state.SelectedFloor);
    }

    /// <summary>Opening an overview card changes browsing, while keeping physical floor and reveal mode intact.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OpensOverviewFloorWithoutChangingRevealMode(bool revealAll)
    {
        var state = new MapViewState { Overview = true, RevealAll = revealAll };
        state.Track(Levels(), 6);

        state.OpenFloor(2, 3);

        Assert.False(state.Overview);
        Assert.Equal(2, state.SelectedFloor);
        Assert.Equal(1, state.CurrentFloor);
        Assert.Equal(revealAll, state.RevealAll);
    }

    /// <summary>A stale thumbnail index cannot leave the overview or select a nonexistent floor.</summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void RejectsInvalidOverviewFloor(int floor)
    {
        var state = new MapViewState { Overview = true };

        state.OpenFloor(floor, 3);

        Assert.True(state.Overview);
        Assert.Equal(0, state.SelectedFloor);
    }

    /// <summary>The overview is opt-in for each dungeon, independent of the persisted reveal preference.</summary>
    [Fact]
    public void EnterRestoresFloorDetail()
    {
        var state = new MapViewState { Overview = true, RevealAll = true };

        state.Enter();

        Assert.False(state.Overview);
        Assert.True(state.RevealAll);
    }

    private static List<float> Levels() => new()
    {
        0,
        6,
        12
    };

    private static NavGraph ExtentGraph()
    {
        var graph = new NavGraph();
        graph.Nodes.Add(new NavNode(new Point(30, 0, -20)) { Floor = 0 });
        graph.Nodes.Add(new NavNode(new Point(40, 0, -10)) { Floor = 0 });
        graph.Nodes.Add(new NavNode(new Point(-40, 12, 40)) { Floor = 1 });
        return graph;
    }
}
