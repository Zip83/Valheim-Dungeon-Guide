using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects rotated containment, room-local anchors and physical layer remapping.</summary>
public sealed class RoomVisitTests
{
    /// <summary>A rotated narrow room cannot mark visits from the unused corners of its world bounds.</summary>
    [Fact]
    public void UsesOrientedRoomInsteadOfWorldBounds()
    {
        const float Diagonal = 0.70710678f;
        var room = new RoomVolume(new Point(10, 0, 10), new Point(Diagonal, 0, Diagonal),
            new Point(0, 1, 0), new Point(-Diagonal, 0, Diagonal), new Point(4, 3, 1));
        Assert.True(room.Contains(new Point(12, 0, 12)));
        Assert.False(room.Contains(new Point(12, 0, 8)));
        Assert.False(room.Contains(new Point(10, 4, 10)));
        Assert.Equal(0, room.Local(new Point(10, 0, 10)).x);
    }

    /// <summary>Adding a lower floor changes its index but preserves the visited physical layer.</summary>
    [Fact]
    public void RemapsEntryHeightWhenLowerFloorsAppear()
    {
        Assert.Equal(0, RoomVisitLayerPolicy.Resolve(new[] { 10f, 14f }, 10.1f, false));
        Assert.Equal(1, RoomVisitLayerPolicy.Resolve(new[] { 6f, 10f, 14f }, 10.1f, false));
    }

    /// <summary>An upper-room entry never marks a downstairs-only provisional map as visited.</summary>
    [Fact]
    public void DoesNotProjectUnknownUpperLayerOntoPreview()
    {
        Assert.Equal(-1, RoomVisitLayerPolicy.Resolve(new[] { 10f }, 14f, true));
        Assert.Equal(1, RoomVisitLayerPolicy.Resolve(new[] { 10f, 14f }, 14f, false));
    }
}
