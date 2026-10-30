using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>
/// Protects chest selection, collection markers, and persistent chest identity.
/// </summary>
public sealed class ChestRegistryTests
{
    /// <summary>
    /// Chest commands use an interactive open or aimed container, never a proximity fallback.
    /// </summary>
    [Theory]
    [InlineData(true, true, true, true, ChestTargetKind.Open)]
    [InlineData(false, true, true, true, ChestTargetKind.Aimed)]
    [InlineData(true, false, true, true, ChestTargetKind.Aimed)]
    [InlineData(false, false, false, true, ChestTargetKind.None)]
    [InlineData(false, false, true, false, ChestTargetKind.None)]
    public void SelectsInteractiveChest(bool open, bool openInRange, bool aimed, bool aimedInRange, ChestTargetKind expected)
    {
        Assert.Equal(expected, new ChestTargetPolicy().Select(open, openInRange, aimed, aimedInRange));
    }

    /// <summary>
    /// Hidden undiscovered chests must not appear as ordinary navigation targets.
    /// </summary>
    [Fact]
    public void ExcludesUnexploredChest()
    {
        var registry = new ChestRegistry(new List<ChestRecord>());
        registry.Observe(new ChestSnapshot { Id = "hidden", Contents = "ore" });
        var known = registry.Observe(new ChestSnapshot { Id = "known", Contents = "wood", Discovered = true });
        Assert.Equal(new[] { known }, registry.Targets());
    }

    /// <summary>
    /// A personal collection marker takes priority over an ordinary known chest.
    /// </summary>
    [Fact]
    public void PrioritizesManualMarker()
    {
        var registry = new ChestRegistry(new List<ChestRecord>());
        var marked = registry.Observe(new ChestSnapshot { Id = "A", Contents = "ore" });
        registry.Observe(new ChestSnapshot { Id = "B", Contents = "wood", Discovered = true });
        registry.Mark(marked);
        Assert.Same(marked, registry.Targets().First());
    }

    /// <summary>
    /// Collecting the actual chest does not clear another chest's marker.
    /// </summary>
    [Fact]
    public void CollectionOnlyAffectsSelectedChest()
    {
        var registry = new ChestRegistry(new List<ChestRecord>());
        var first = registry.Observe(new ChestSnapshot { Id = "A" });
        var second = registry.Observe(new ChestSnapshot { Id = "B" });
        registry.Mark(first);
        registry.Mark(second);
        registry.Collect(first);
        Assert.True(first.done && !first.collect && second.collect && !second.done);
    }

    /// <summary>
    /// A chest can be marked again after previously confirming collection.
    /// </summary>
    [Fact]
    public void CanMarkCollectedChestAgain()
    {
        var registry = new ChestRegistry(new List<ChestRecord>());
        var chest = registry.Observe(new ChestSnapshot { Id = "A" });
        registry.Collect(chest);
        registry.Mark(chest);
        Assert.True(chest.collect && !chest.done);
    }

    /// <summary>
    /// Adopting the stable network identifier preserves existing notes and markers.
    /// </summary>
    [Fact]
    public void MigratesChestIdentityWithoutLosingNotes()
    {
        var old = new ChestRecord
        {
            id = "old-C",
            label = "My ore",
            contents = "ore",
            collect = true
        };
        var registry = new ChestRegistry(new List<ChestRecord> { old });
        var migrated = registry.Observe(new ChestSnapshot { Id = "zdo-C", LegacyId = "old-C", Contents = "ore" });
        Assert.Same(old, migrated);
        Assert.Equal("zdo-C", migrated.id);
        Assert.Equal("My ore", migrated.label);
        Assert.True(migrated.collect);
    }

    /// <summary>
    /// Unavailable remote inventory data must not erase a previously known snapshot.
    /// </summary>
    [Fact]
    public void PreservesContentsWhenSnapshotUnavailable()
    {
        var registry = new ChestRegistry(new List<ChestRecord>());
        var chest = registry.Observe(new ChestSnapshot { Id = "C", Contents = "ore" });
        registry.Observe(new ChestSnapshot { Id = "C", Contents = null });
        Assert.Equal("ore", chest.contents);
    }
}
