using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Upgrades supported save schemas while rejecting unknown future formats and corrupt return locations.</summary>
public sealed class SaveMigration
{
    /// <summary>Upgrades legacy state to schema 4 without inferring visits or rewriting names and coordinates.</summary>
    /// <param name="data">Loaded dungeon state, upgraded in place only after version validation.</param>
    /// <returns>The same supported state with initialized collections.</returns>
    /// <exception cref="InvalidOperationException">The schema is newer or personal locations are corrupt.</exception>
    public SavedRun Upgrade(SavedRun data)
    {
        if (data.version > 4)
        {
            throw new InvalidOperationException("The saved data requires a newer plugin version.");
        }

        InitializeCollections(data);
        ValidateWaypoints(data.waypoints);
        MigrateLegacyChests(data);
        data.version = 4;
        return data;
    }

    private static void InitializeCollections(SavedRun data)
    {
        data.chests ??= new List<ChestRecord>();
        data.visited ??= new List<string>();
        data.traversals ??= new List<TraversalProof>();
        data.visitedRooms ??= new List<RoomVisit>();
        data.waypoints ??= new List<WaypointRecord>();
    }

    private static void ValidateWaypoints(IEnumerable<WaypointRecord> records)
    {
        var identities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var record in records)
        {
            if (!WaypointRegistry.IsValid(record) || !identities.Add(record.id))
            {
                throw new InvalidOperationException("The saved return locations contain invalid or duplicate records.");
            }
        }
    }

    private static void MigrateLegacyChests(SavedRun data)
    {
        if (data.version >= 2)
        {
            return;
        }

        foreach (var chest in data.chests)
        {
            chest.discovered = chest.collect || chest.done;
        }
    }
}
