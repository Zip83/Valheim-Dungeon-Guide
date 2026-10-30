using System;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>
/// Freezes the existing geometric jump envelope and clearance curve while scan responsibilities are separated.
/// </summary>
public sealed class JumpCandidateGeometryTests
{
    /// <summary>
    /// Takeoff distance and landing elevation retain their inclusive candidate limits.
    /// </summary>
    /// <param name="distance">Horizontal gap distance in metres.</param>
    /// <param name="rise">Landing elevation relative to takeoff in metres.</param>
    /// <param name="expected">Whether the measurements qualify as a geometric candidate.</param>
    [Theory]
    [InlineData(1.49f, 0f, false)]
    [InlineData(1.5f, 0f, true)]
    [InlineData(2.8f, 0f, true)]
    [InlineData(2.81f, 0f, false)]
    [InlineData(2f, 0.9f, true)]
    [InlineData(2f, 0.901f, false)]
    [InlineData(2f, -2f, true)]
    [InlineData(2f, -2.001f, false)]
    public void PreservesCandidateLimits(float distance, float rise, bool expected)
    {
        Assert.Equal(expected, JumpCandidateGeometry.WithinRange(distance, rise));
    }

    /// <summary>
    /// The curve retains endpoint clearance and its raised midpoint above the interpolated floor heights.
    /// </summary>
    /// <param name="progress">Normalized position on the sampled arc.</param>
    /// <param name="expected">Expected clearance above the interpolated floor heights in metres.</param>
    [Theory]
    [InlineData(0f, 0.8f)]
    [InlineData(0.5f, 1.7f)]
    [InlineData(1f, 0.8f)]
    public void PreservesArcClearance(float progress, float expected)
    {
        Assert.InRange(Math.Abs(JumpCandidateGeometry.ArcLift(progress) - expected), 0, 0.0001f);
    }
}
