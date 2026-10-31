using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects existing fog keys and independently persisted actual room visits.</summary>
public sealed class RoomVisitMigrationTests
{
    /// <summary>Schema 2 retains all existing state without claiming any whole-room visit.</summary>
    [Fact]
    public void MigratesVersionTwoWithoutInferringRooms()
    {
        var data = new SavedRun { version = 2, visitedRooms = null };
        data.visited.Add("known");
        var result = new SaveMigration().Upgrade(data);
        Assert.Same(data, result);
        Assert.Equal(4, result.version);
        Assert.Equal("known", Assert.Single(result.visited));
        Assert.Empty(result.visitedRooms);
    }

    /// <summary>Stable room-local anchors survive migration without depending on current floor numbers.</summary>
    [Fact]
    public void PreservesRoomVisitAnchors()
    {
        var visit = new RoomVisit { room = "room-pose", local = new Point(2, 4, 1) };
        var data = new SavedRun();
        data.visitedRooms.Add(visit);
        Assert.Same(visit, Assert.Single(new SaveMigration().Upgrade(data).visitedRooms));
    }

}
