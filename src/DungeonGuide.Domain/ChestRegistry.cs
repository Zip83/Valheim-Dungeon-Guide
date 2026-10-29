using System.Collections.Generic;
using System.Linq;

namespace DungeonGuide;

/// <summary>
/// Maintains chest identity and personal collection markers without modifying inventories.
/// </summary>
public sealed class ChestRegistry
{
    private readonly List<ChestRecord> _records;

    /// <summary>
    /// Uses the session-owned chest records so observations and marker changes remain persistent.
    /// </summary>
    /// <param name="records">Session-owned persistent chest records, updated in place.</param>
    public ChestRegistry(List<ChestRecord> records)
    {
        _records = records;
    }

    /// <summary>
    /// Merges an observation, upgrading legacy identity while preserving notes and unavailable inventory data.
    /// </summary>
    /// <param name="snapshot">Locally observed chest identity, position, and optional contents.</param>
    /// <returns>The merged persistent chest record, retaining its previous notes and markers.</returns>
    public ChestRecord Observe(ChestSnapshot snapshot)
    {
        var record = _records.FirstOrDefault(c => c.id == snapshot.Id || c.id == snapshot.LegacyId);
        if (record == null)
        {
            record = new ChestRecord
            {
                label = GuideText.ChestPrefix + (_records.Count + 1)
            };
            _records.Add(record);
        }

        record.id = snapshot.Id;
        record.position = snapshot.Position;
        if (snapshot.Contents != null)
        {
            record.contents = snapshot.Contents;
        }

        record.discovered |= snapshot.Discovered;
        return record;
    }

    /// <summary>
    /// Marks the chest for collection and makes it known, including after an earlier collection.
    /// </summary>
    /// <param name="record">Chest record whose personal collection state is being changed.</param>
    public void Mark(ChestRecord record)
    {
        record.collect = true;
        record.done = false;
        record.discovered = true;
    }

    /// <summary>
    /// Confirms collection of this record without changing unrelated chest markers.
    /// </summary>
    /// <param name="record">Chest record whose personal collection state is being changed.</param>
    public void Collect(ChestRecord record)
    {
        record.collect = false;
        record.done = true;
        record.discovered = true;
    }

    /// <summary>
    /// Returns known, uncollected targets with personal collection markers first, ordered by label.
    /// </summary>
    /// <returns>Eligible known chest records ordered by personal priority and label.</returns>
    public List<ChestRecord> Targets() =>
        _records.Where(c => !c.done && (c.collect || c.discovered && !string.IsNullOrEmpty(c.contents)))
        .OrderByDescending(c => c.collect)
        .ThenBy(c => c.label)
        .ToList();
}
