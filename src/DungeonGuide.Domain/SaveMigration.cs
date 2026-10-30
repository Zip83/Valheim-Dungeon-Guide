using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Upgrades supported save schemas while rejecting unknown future formats.
/// </summary>
public sealed class SaveMigration
{
    /// <summary>
    /// Updates legacy exploration and chest state to schema 2, initializing missing traversal records without erasing
    /// notes.
    /// </summary>
    /// <param name="data">Loaded dungeon state that may be upgraded in place.</param>
    /// <returns>The supported state upgraded in place to the current save schema.</returns>
    /// <exception cref="System.InvalidOperationException">The save uses an unsupported future schema.</exception>
    public SavedRun Upgrade(SavedRun data)
    {
        if (data.version > 2)
        {
            throw new InvalidOperationException("The saved data requires a newer plugin version.");
        }

        if (data.chests == null)
        {
            data.chests = new List<ChestRecord>();
        }

        if (data.visited == null)
        {
            data.visited = new List<string>();
        }

        if (data.traversals == null)
        {
            data.traversals = new List<TraversalProof>();
        }

        if (data.version < 2)
        {
            foreach (var chest in data.chests)
            {
                chest.discovered = chest.collect || chest.done;
            }
        }

        data.version = 2;
        return data;
    }
}
