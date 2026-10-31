using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>
/// Protects legacy saves and rejects formats the current plugin cannot safely write.
/// </summary>
public sealed class SaveCompatibilityTests
{
    /// <summary>
    /// Legacy migration retains exploration and manually recorded collection state.
    /// </summary>
    [Fact]
    public void UpgradesLegacySaveWithoutDataLoss()
    {
        var old = new SavedRun
        {
            version = 1,
            visited = new List<string>
            {
                "known"
            },
            chests = new List<ChestRecord>
            {
                new()
                {
                    id = "old",
                    label = "Ore",
                    collect = true
                }
            },
            traversals = null
        };
        var upgraded = new SaveMigration().Upgrade(old);
        Assert.Equal(4, upgraded.version);
        Assert.Empty(upgraded.visitedRooms);
        Assert.Equal("known", Assert.Single(upgraded.visited));
        var chest = Assert.Single(upgraded.chests);
        Assert.Equal("Ore", chest.label);
        Assert.True(chest.collect && chest.discovered);
        Assert.NotNull(upgraded.traversals);
    }

    /// <summary>
    /// A future save format is rejected instead of being overwritten as empty state.
    /// </summary>
    [Fact]
    public void RejectsFutureSaveFormat()
    {
        Assert.Throws<InvalidOperationException>(() => new SaveMigration().Upgrade(new SavedRun { version = 99 }));
    }

    /// <summary>
    /// Exploration reload does not reveal vertically overlapping floor geometry.
    /// </summary>
    [Fact]
    public void ReloadsExplorationWithoutRevealingOtherFloor()
    {
        var original = new Exploration();
        original.Reveal(new Point(4, 0, 4));
        var loaded = new Exploration();
        loaded.Load(original.Export());
        Assert.True(loaded.Has(new Point(4, 0, 4)));
        Assert.False(loaded.Has(new Point(4, 6, 4)));
    }

    /// <summary>
    /// Serialized field names are wire contracts and must survive formatting and refactoring.
    /// </summary>
    [Theory]
    [InlineData(typeof(Point), "x,y,z")]
    [InlineData(typeof(RoomVisit), "local,room")]
    [InlineData(typeof(ChestRecord), "collect,contents,discovered,done,id,label,position")]
    [InlineData(typeof(SavedRun), "chests,dungeon,start,traversals,version,visited,visitedRooms,waypoints,world")]
    [InlineData(typeof(TraversalProof), "from,jumpForce,jumpSkill,to")]
    public void PreservesSerializedFieldNames(Type type, string expected)
    {
        var names = type.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(f => f.Name)
            .OrderBy(
                n => n,
                StringComparer.Ordinal);
        Assert.Equal(expected, string.Join(",", names));
    }
}
