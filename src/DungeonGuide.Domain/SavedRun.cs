using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Stores exploration and chest state for one world and dungeon using the existing JSON schema.
/// </summary>
[Serializable]
public sealed class SavedRun
{
    /// <summary>
    /// Serialized schema version, currently 2; this is independent of the plugin release version.
    /// </summary>
    public int version = 2;

    /// <summary>
    /// World identifier used to keep exploration from different worlds separate.
    /// </summary>
    public string world;

    /// <summary>
    /// Stable dungeon identifier used with the world to select the save file.
    /// </summary>
    public string dungeon;

    /// <summary>
    /// Quantized floor sample keys already explored by the player.
    /// </summary>
    public List<string> visited = new();

    /// <summary>
    /// Known container identities, player notes, and collection markers.
    /// </summary>
    public List<ChestRecord> chests = new();

    /// <summary>
    /// Observed directional jump and drop evidence retained for later validation.
    /// </summary>
    public List<TraversalProof> traversals = new();

    /// <summary>
    /// Recorded scan origin used only as a labeled fallback when no entrance is known.
    /// </summary>
    public Point start;
}
