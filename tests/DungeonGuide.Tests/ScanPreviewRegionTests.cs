using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects bounded initial sampling without assuming flat architectural floors.</summary>
public sealed class ScanPreviewRegionTests
{
    /// <summary>A tall fortress initially samples the player's elevation rather than its roof or bottom.</summary>
    [Fact]
    public void PrioritizesCurrentElevation()
    {
        var region = ScanPreviewRegion.Create(new Point(-100, 4900, -100), new Point(100, 5100, 100),
            new Point(20, 4980, -30));

        Assert.Equal(4976, region.Minimum.y);
        Assert.Equal(4984, region.Maximum.y);
        Assert.Equal(4, region.Minimum.x);
        Assert.Equal(36, region.Maximum.x);
        Assert.Equal(-46, region.Minimum.z);
        Assert.Equal(-14, region.Maximum.z);
    }

    /// <summary>Small or edge rooms must not cause sampling outside the dungeon's original bounds.</summary>
    [Fact]
    public void ClipsNeighborhoodToDungeonBounds()
    {
        var region = ScanPreviewRegion.Create(new Point(0, 0, 0), new Point(10, 6, 10), new Point(1, 1, 9));

        Assert.Equal(new Point(0, 0, 0), region.Minimum);
        Assert.Equal(new Point(10, 5, 10), region.Maximum);
    }

    /// <summary>A stale player location cannot select a neighborhood outside the detected dungeon.</summary>
    [Theory]
    [InlineData(-1, 5, 5)]
    [InlineData(5, 11, 5)]
    [InlineData(5, 5, 11)]
    public void RejectsPlayerOutsideDungeon(float x, float y, float z)
    {
        Assert.Null(ScanPreviewRegion.Create(new Point(0, 0, 0), new Point(10, 10, 10), new Point(x, y, z)));
    }
}
