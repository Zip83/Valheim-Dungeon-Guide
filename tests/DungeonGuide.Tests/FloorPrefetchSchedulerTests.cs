using System.Collections.Generic;
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

        Assert.False(scheduler.TrySelect(new[] { 1, 2, 0 }, 3, false, _ => false, budget, out _));
        Assert.Equal(0, budget.TotalSteps);
    }

    /// <summary>Prefetch cannot reset or exceed capacity already consumed by the foreground map.</summary>
    [Fact]
    public void DoesNotExceedSharedBudget()
    {
        var scheduler = new FloorPrefetchScheduler();
        var budget = new FrameWorkBudget(1, 1000, () => 0);
        Assert.True(budget.TryStartWork());

        Assert.False(scheduler.TrySelect(new[] { 1, 2, 0 }, 3, true, _ => false, budget, out _));
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
            Assert.True(scheduler.TrySelect(new[] { 1, 2, 0 }, 3, true, _ => false, budget, out floors[i]));
        }

        Assert.Equal(new[] { 2, 0, 2, 0 }, floors);
    }

    /// <summary>A single-floor dungeon needs no background work or invalid neighboring index.</summary>
    [Fact]
    public void SkipsSingleFloorDungeon()
    {
        var scheduler = new FloorPrefetchScheduler();
        var budget = new FrameWorkBudget(10, 1000, () => 0);

        Assert.False(scheduler.TrySelect(new[] { 0 }, 1, true, _ => true, budget, out var floor));
        Assert.Equal(-1, floor);
        Assert.Equal(0, budget.TotalSteps);
    }

    /// <summary>Distant construction cannot consume background slices before both physical neighbors are ready.</summary>
    [Fact]
    public void FinishesPriorityNeighborhoodBeforeDistantWarmup()
    {
        var scheduler = new FloorPrefetchScheduler();
        var ready = new HashSet<int> { 1 };
        var targets = new[] { 1, 2, 0, 3, 4 };
        var budget = new FrameWorkBudget(20, 1000, () => 0);
        Assert.True(scheduler.TrySelect(targets, 3, true, ready.Contains, budget, out var first));
        Assert.Equal(2, first);
        ready.Add(2);
        Assert.True(scheduler.TrySelect(targets, 3, true, ready.Contains, budget, out var second));
        Assert.Equal(0, second);
        ready.Add(0);

        Assert.True(scheduler.TrySelect(targets, 3, true, ready.Contains, budget, out var distant));
        Assert.Equal(3, distant);
    }

    /// <summary>Distant floors progress in order while ready floors keep receiving discovery updates.</summary>
    [Fact]
    public void AlternatesDistantWarmupAndReadyRefresh()
    {
        var scheduler = new FloorPrefetchScheduler();
        var ready = new HashSet<int> { 1, 2, 0 };
        var targets = new[] { 1, 2, 0, 3, 4 };
        var budget = new FrameWorkBudget(20, 1000, () => 0);
        Assert.True(scheduler.TrySelect(targets, 3, true, ready.Contains, budget, out var first));
        Assert.Equal(3, first);
        Assert.True(scheduler.TrySelect(targets, 3, true, ready.Contains, budget, out var refresh));
        Assert.Equal(2, refresh);
        Assert.True(scheduler.TrySelect(targets, 3, true, ready.Contains, budget, out var pending));
        Assert.Equal(3, pending);
        ready.Add(3);
        Assert.True(scheduler.TrySelect(targets, 3, true, ready.Contains, budget, out var secondRefresh));
        Assert.Equal(0, secondRefresh);
        Assert.True(scheduler.TrySelect(targets, 3, true, ready.Contains, budget, out var next));
        Assert.Equal(4, next);
    }

    /// <summary>After all floors are warm, each retained background floor is maintained without starvation.</summary>
    [Fact]
    public void RefreshesEveryReadyFloor()
    {
        var scheduler = new FloorPrefetchScheduler();
        var targets = new[] { 1, 2, 0, 3, 4 };
        var budget = new FrameWorkBudget(20, 1000, () => 0);
        var actual = new int[8];
        for (var index = 0; index < actual.Length; index++)
        {
            Assert.True(scheduler.TrySelect(targets, 3, true, _ => true, budget, out actual[index]));
        }

        Assert.Equal(new[] { 2, 0, 3, 4, 2, 0, 3, 4 }, actual);
    }

    /// <summary>An exhausted frame does not rotate away from the next priority neighbor.</summary>
    [Fact]
    public void BudgetDenialPreservesPendingPriority()
    {
        var scheduler = new FloorPrefetchScheduler();
        var targets = new[] { 1, 2, 0, 3 };
        var denied = new FrameWorkBudget(1, 1000, () => 0);
        Assert.True(denied.TryStartWork());
        Assert.False(scheduler.TrySelect(targets, 3, true, _ => false, denied, out _));

        var nextFrame = new FrameWorkBudget(1, 1000, () => 0);
        Assert.True(scheduler.TrySelect(targets, 3, true, _ => false, nextFrame, out var next));
        Assert.Equal(2, next);
    }

}
