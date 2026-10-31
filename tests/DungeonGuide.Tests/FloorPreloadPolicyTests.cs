using System.Collections.Generic;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects edge floors, selected-floor priority and the bounded physical-floor neighborhood.</summary>
public sealed class FloorPreloadPolicyTests
{
    /// <summary>Only existing distinct floors enter the working set, with the viewed floor first.</summary>
    [Theory]
    [InlineData(0, 0, 0, new int[0])]
    [InlineData(0, 0, 1, new[] { 0 })]
    [InlineData(0, 0, 3, new[] { 0, 1 })]
    [InlineData(2, 2, 3, new[] { 2, 1 })]
    [InlineData(1, 1, 3, new[] { 1, 2, 0 })]
    [InlineData(2, 4, 6, new[] { 4, 2, 3, 1 })]
    [InlineData(-1, 2, 4, new[] { 2 })]
    [InlineData(2, -1, 4, new[] { 2, 3, 1 })]
    [InlineData(1, 99, 3, new[] { 1, 2, 0 })]
    [InlineData(99, 99, 3, new int[0])]
    public void SelectsOnlyValidFloors(int physical, int selected, int count, int[] expected)
    {
        var targets = new List<int> { 100 };
        FloorPreloadPolicy.Fill(targets, physical, selected, count);

        Assert.Equal(expected, targets);
    }
}
