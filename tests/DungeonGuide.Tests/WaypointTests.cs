using System;
using System.Collections.Generic;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects personal locations, independent identities, persisted physical layers and save compatibility.</summary>
public sealed class WaypointTests
{
    /// <summary>Two locations at the same horizontal point keep different identities and physical elevations.</summary>
    [Fact]
    public void StoresIndependentPhysicalLocations()
    {
        var records = new List<WaypointRecord>();
        var registry = new WaypointRegistry(records);
        var low = registry.Add(new Point(2, 0, 3), " My iron ");
        var high = registry.Add(new Point(2, 6, 3), "Bag upstairs");

        Assert.NotEqual(low.id, high.id);
        Assert.Equal("My iron", low.label);
        Assert.Equal(6, high.position.y);
        Assert.Same(low, registry.Next(null));
        Assert.Same(high, registry.Next(low));
        Assert.Same(low, registry.Next(high));
    }

    /// <summary>Renaming/removing a selected marker preserves unrelated locations and the selected identity/position.</summary>
    [Fact]
    public void EditsOnlyOwnedLocation()
    {
        var registry = new WaypointRegistry(new List<WaypointRecord>());
        var first = registry.Add(new Point(1, 2, 3), "Bag");
        var second = registry.Add(new Point(), "Other");
        var id = first.id;
        Assert.True(registry.Rename(first, "Iron"));
        Assert.False(registry.Rename(first, " "));
        Assert.Equal("Iron", first.label);
        Assert.Equal(id, first.id);
        Assert.Equal(new Point(1, 2, 3), first.position);
        Assert.True(registry.Remove(first));
        Assert.False(registry.Remove(first));
        Assert.Same(second, registry.Next(first));
        Assert.False(registry.Rename(first, "Stale dialog"));
    }

    /// <summary>Empty registries have no navigation target.</summary>
    [Fact]
    public void EmptyRegistryHasNoTarget()
    {
        Assert.Null(new WaypointRegistry(new List<WaypointRecord>()).Next(null));
    }

    /// <summary>Unserializable coordinates cannot become return destinations.</summary>
    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void RejectsNonFinitePositions(float height)
    {
        var records = new List<WaypointRecord>();
        Assert.Throws<ArgumentException>(() => new WaypointRegistry(records).Add(new Point(1, height, 2), "Bag"));
        Assert.Empty(records);
    }

    /// <summary>Legacy schema 3 acquires an empty location list while preserving all existing state.</summary>
    [Fact]
    public void MigratesWithoutInventingLocations()
    {
        var data = new SavedRun { version = 3, waypoints = null };
        var chest = new ChestRecord { id = "chest", label = "My note" };
        data.chests.Add(chest);
        data.visited.Add("floor-key");
        var room = new RoomVisit { room = "room", local = new Point(1, 2, 3) };
        data.visitedRooms.Add(room);
        var result = new SaveMigration().Upgrade(data);

        Assert.Equal(4, result.version);
        Assert.Empty(result.waypoints);
        Assert.Same(chest, Assert.Single(result.chests));
        Assert.Same(room, Assert.Single(result.visitedRooms));
        Assert.Equal("floor-key", Assert.Single(result.visited));
    }

    /// <summary>Persisted player names and heights survive reload without depending on floor numbering.</summary>
    [Fact]
    public void PreservesExistingLocations()
    {
        var data = new SavedRun();
        var record = new WaypointRegistry(data.waypoints).Add(new Point(4, 12, 8), "Moje železo");
        Assert.Same(record, Assert.Single(new SaveMigration().Upgrade(data).waypoints));
        Assert.Equal("Moje železo", record.label);
        Assert.Equal(12, record.position.y);
    }

    /// <summary>Duplicate stored identities are rejected instead of silently losing a player's locations.</summary>
    [Fact]
    public void RejectsCorruptDuplicateIdentities()
    {
        var data = new SavedRun();
        var record = new WaypointRegistry(data.waypoints).Add(new Point(), "Bag");
        data.waypoints.Add(new WaypointRecord { id = record.id, label = "Other", position = new Point() });
        Assert.Throws<InvalidOperationException>(() => new SaveMigration().Upgrade(data));
        Assert.Equal(2, data.waypoints.Count);
    }
}
