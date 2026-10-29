using System;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>
/// Protects cooperative scan fairness independently of machine speed or Unity frame timing.
/// </summary>
public sealed class FrameWorkBudgetTests
{
    /// <summary>
    /// Cheap operations still yield at the work limit even when the clock has not advanced.
    /// </summary>
    [Fact]
    public void LimitsStepsWhenTimeDoesNotAdvance()
    {
        var budget = new FrameWorkBudget(2, 1, () => 0);

        Assert.True(budget.TryStartWork());
        Assert.True(budget.TryStartWork());
        Assert.False(budget.TryStartWork());
        Assert.False(budget.TryStartWork());
    }

    /// <summary>
    /// Reaching the elapsed-time limit prevents another step before the count limit is reached.
    /// </summary>
    [Fact]
    public void StopsAtTimeLimit()
    {
        var now = 100.0;
        var budget = new FrameWorkBudget(100, 1, () => now);
        Assert.True(budget.TryStartWork());

        now = 101;
        Assert.False(budget.TryStartWork());
    }

    /// <summary>
    /// A slow indivisible query cannot be preempted, but it prevents further work in the same frame.
    /// </summary>
    [Fact]
    public void YieldsAfterAnIndivisibleStepOverruns()
    {
        var now = 0.0;
        var budget = new FrameWorkBudget(100, 1, () => now);
        Assert.True(budget.TryStartWork());

        now = 7;
        Assert.False(budget.TryStartWork());
    }

    /// <summary>
    /// Resuming after a yield resets both limits rather than counting suspended game time as scan work.
    /// </summary>
    [Fact]
    public void ResetsOnlyWhenResumingANewFrame()
    {
        var now = 0.0;
        var budget = new FrameWorkBudget(1, 1, () => now);
        Assert.True(budget.TryStartWork());
        Assert.False(budget.TryStartWork());

        now = 100;
        budget.BeginFrame();
        Assert.True(budget.TryStartWork());
        Assert.False(budget.TryStartWork());
    }

    /// <summary>
    /// Invalid limits are rejected rather than allowing an infinite no-progress yield loop.
    /// </summary>
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(1, 0)]
    [InlineData(1, -1)]
    [InlineData(1, double.NaN)]
    [InlineData(1, double.PositiveInfinity)]
    public void RejectsInvalidLimits(int steps, double milliseconds)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FrameWorkBudget(steps, milliseconds));
    }
}
