using System.Collections.Generic;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects edge floors, selected-floor priority and complete distance-ordered dungeon coverage.</summary>
public sealed class FloorPreloadPolicyTests
{
    /// <summary>Every existing floor appears once, with viewed/physical neighbors forming the priority prefix.</summary>
    [Theory]
    [InlineData(0, 0, 0, new int[0], 0)]
    [InlineData(0, 0, 1, new[] { 0 }, 1)]
    [InlineData(0, 0, 3, new[] { 0, 1, 2 }, 2)]
    [InlineData(2, 2, 3, new[] { 2, 1, 0 }, 2)]
    [InlineData(1, 1, 3, new[] { 1, 2, 0 }, 3)]
    [InlineData(2, 4, 6, new[] { 4, 2, 3, 1, 0, 5 }, 4)]
    [InlineData(-1, 2, 4, new[] { 2, 3, 1, 0 }, 1)]
    [InlineData(2, -1, 4, new[] { 2, 3, 1, 0 }, 3)]
    [InlineData(1, 99, 3, new[] { 1, 2, 0 }, 3)]
    [InlineData(99, 99, 3, new int[0], 0)]
    public void SelectsOnlyValidFloors(int physical, int selected, int count, int[] expected, int priority)
    {
        var targets = new List<int> { 100 };
        var actualPriority = FloorPreloadPolicy.Fill(targets, physical, selected, count);

        Assert.Equal(expected, targets);
        Assert.Equal(priority, actualPriority);
    }
}
