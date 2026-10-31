using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects foreground priority, shared frame limits and fair neighbor warmup.</summary>
public sealed class FloorPrefetchSchedulerTests
{
    /// <summary>Preparing the first foreground image never spends work on adjacent floors.</summary>
    [Fact]
    public void WaitsForForegroundReadiness()
    {
        var scheduler = new FloorPrefetchScheduler();
        var budget = new FrameWorkBudget(10, 1000, () => 0);

        Assert.False(scheduler.TrySelect(new[] { 1, 2, 0 }, false, budget, out _));
        Assert.Equal(0, budget.TotalSteps);
    }

    /// <summary>Prefetch cannot reset or exceed capacity already consumed by the foreground map.</summary>
    [Fact]
    public void DoesNotExceedSharedBudget()
    {
        var scheduler = new FloorPrefetchScheduler();
        var budget = new FrameWorkBudget(1, 1000, () => 0);
        Assert.True(budget.TryStartWork());

        Assert.False(scheduler.TrySelect(new[] { 1, 2, 0 }, true, budget, out _));
        Assert.Equal(1, budget.TotalSteps);
    }

    /// <summary>One unfinished neighbor cannot starve the other across consecutive admitted slices.</summary>
    [Fact]
    public void AlternatesAvailableNeighbors()
    {
        var scheduler = new FloorPrefetchScheduler();
        var budget = new FrameWorkBudget(10, 1000, () => 0);
        var floors = new int[4];
        for (var i = 0; i < floors.Length; i++)
        {
            Assert.True(scheduler.TrySelect(new[] { 1, 2, 0 }, true, budget, out floors[i]));
        }

        Assert.Equal(new[] { 2, 0, 2, 0 }, floors);
    }

    /// <summary>A single-floor dungeon needs no background work or invalid neighboring index.</summary>
    [Fact]
    public void SkipsSingleFloorDungeon()
    {
        var scheduler = new FloorPrefetchScheduler();
        var budget = new FrameWorkBudget(10, 1000, () => 0);

        Assert.False(scheduler.TrySelect(new[] { 0 }, true, budget, out var floor));
        Assert.Equal(-1, floor);
        Assert.Equal(0, budget.TotalSteps);
    }
}
