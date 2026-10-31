using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects world-space crop alignment when a ready full image backs a discovered-only thumbnail.</summary>
public sealed class OverviewFrameTests
{
    /// <summary>A frame projected into its own raster fills the thumbnail exactly.</summary>
    [Fact]
    public void MatchingRasterUsesWholeImage()
    {
        var frame = new OverviewFrame(new OverviewFloor(2, new Point(10, 3, 20)), 1);
        var raster = new FloorPlanRaster(192, 2, frame.Center, frame.Span);

        Assert.True(frame.Matches(raster));
        Assert.Equal((0f, 0f, 1f), frame.Crop(raster));
    }

    /// <summary>The target center stays centered in the crop rather than exposing the full geometry extent.</summary>
    [Fact]
    public void KnownCenterAlignsInsideFullRaster()
    {
        var frame = new OverviewFrame(new OverviewFloor(0, new Point(10, 0, 5)), 1);
        var full = new FloorPlanRaster(192, 0, new Point(), 40);
        var crop = frame.Crop(full);

        Assert.Equal(0.75f, crop.X + crop.Size / 2, 5);
        Assert.Equal(0.625f, crop.Y + crop.Size / 2, 5);
        Assert.Equal(3f / 40, crop.Size, 5);
    }

    /// <summary>Long known corridors retain their conservative square, with outside pixels covered by fog.</summary>
    [Fact]
    public void DoesNotClampKnownFramingToUndiscoveredBounds()
    {
        var known = new OverviewFloor(0, new Point());
        known.Include(new Point(100, 0, 1));
        var frame = new OverviewFrame(known, 1);
        var full = new FloorPlanRaster(192, 0, new Point(50, 0, 50), 103);

        Assert.True(frame.Crop(full).Y < 0);
        Assert.Equal(1f, frame.Crop(full).Size);
        Assert.Equal(0.5f, frame.Center.z);
    }
}
