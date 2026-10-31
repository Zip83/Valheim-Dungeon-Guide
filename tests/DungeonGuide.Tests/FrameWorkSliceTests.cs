using System;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects guaranteed background shares without adding a second frame allowance.</summary>
public sealed class FrameWorkSliceTests
{
    /// <summary>Background step reservations leave foreground capacity and count against the original ceiling.</summary>
    [Fact]
    public void SliceReservesOnlyItsShareOfParentSteps()
    {
        var budget = new FrameWorkBudget(10, 1, () => 0);
        var background = budget.CreateSlice(0.3);
        for (var i = 0; i < 3; i++)
        {
            Assert.True(background.TryStartWork());
        }

        Assert.False(background.TryStartWork());
        Assert.Equal(3, budget.TotalSteps);
        for (var i = 0; i < 7; i++)
        {
            Assert.True(budget.TryStartWork());
        }

        Assert.False(budget.TryStartWork());
        Assert.Equal(10, budget.TotalSteps);
    }

    /// <summary>Reaching the background time slice still leaves the rest of the original millisecond for foreground work.</summary>
    [Fact]
    public void SliceTimeLimitPreservesForegroundAllowance()
    {
        var now = 0d;
        var budget = new FrameWorkBudget(100, 1, () => now);
        var background = budget.CreateSlice(0.35);
        Assert.True(background.TryStartWork());
        now = 0.35;
        Assert.False(background.TryStartWork());
        Assert.True(budget.TryStartWork());
        now = 1;
        Assert.False(budget.TryStartWork());
        Assert.False(budget.CreateSlice(1).TryStartWork());
    }

    /// <summary>Multiple consumers or resetting a local view cannot replenish a parent already exhausted by work.</summary>
    [Fact]
    public void ResettingSlicesCannotExceedParentCapacity()
    {
        var budget = new FrameWorkBudget(2, 1, () => 0);
        var background = budget.CreateSlice(0.5);
        Assert.True(background.TryStartWork());
        background.BeginFrame();
        Assert.True(background.TryStartWork());
        background.BeginFrame();
        Assert.False(background.TryStartWork());
        Assert.False(budget.CreateSlice(1).TryStartWork());
        Assert.Equal(2, budget.TotalSteps);
    }

    /// <summary>An indivisible background overrun stops both consumers for the rest of the shared frame.</summary>
    [Fact]
    public void BackgroundOverrunStopsAllFurtherFrameWork()
    {
        var now = 0d;
        var budget = new FrameWorkBudget(100, 1, () => now);
        var background = budget.CreateSlice(0.35);
        Assert.True(background.TryStartWork());
        now = 4;
        Assert.False(background.TryStartWork());
        Assert.False(budget.TryStartWork());
        Assert.Equal(1, budget.TotalSteps);
    }

    /// <summary>A view cannot request an invalid or larger-than-parent fraction.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void RejectsInvalidShares(double fraction)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FrameWorkBudget(10, 1).CreateSlice(fraction));
    }
}
