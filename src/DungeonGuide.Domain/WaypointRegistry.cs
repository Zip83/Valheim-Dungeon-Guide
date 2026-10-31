using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Maintains named return locations in their owning dungeon save without changing exploration.</summary>
internal sealed class WaypointRegistry
{
    private readonly List<WaypointRecord> _records;

    /// <summary>Uses the save-owned list directly so mutations participate in existing atomic persistence.</summary>
    public WaypointRegistry(List<WaypointRecord> records)
    {
        _records = records;
    }

    /// <summary>Adds a distinctly identified location, preserving its exact physical coordinates.</summary>
    /// <exception cref="ArgumentException">The name or position cannot be persisted safely.</exception>
    public WaypointRecord Add(Point position, string label)
    {
        var record = new WaypointRecord { id = Guid.NewGuid().ToString("N"), label = label?.Trim(), position = position };
        if (!IsValid(record))
        {
            throw new ArgumentException("Waypoint requires a name of 1–80 characters and finite coordinates.");
        }

        _records.Add(record);
        return record;
    }

    /// <summary>Updates only a name belonging to this registry; rejects blank or oversized names.</summary>
    public bool Rename(WaypointRecord record, string label)
    {
        if (!_records.Contains(record) || !ValidLabel(label?.Trim()))
        {
            return false;
        }

        record.label = label.Trim();
        return true;
    }

    /// <summary>Removes only the exact selected location, leaving other markers unchanged.</summary>
    public bool Remove(WaypointRecord record) => _records.Remove(record);

    /// <summary>Cycles saved creation order across floors; null or stale selection starts with the first location.</summary>
    public WaypointRecord Next(WaypointRecord current)
    {
        if (_records.Count == 0)
        {
            return null;
        }

        return _records[(_records.IndexOf(current) + 1) % _records.Count];
    }

    /// <summary>Validates stored scalar fields without changing player names or physical coordinates.</summary>
    public static bool IsValid(WaypointRecord record) => record != null && !string.IsNullOrWhiteSpace(record.id)
        && ValidLabel(record.label) && Finite(record.position.x) && Finite(record.position.y) && Finite(record.position.z);

    private static bool ValidLabel(string label) => !string.IsNullOrWhiteSpace(label) && label.Length <= 80;

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
