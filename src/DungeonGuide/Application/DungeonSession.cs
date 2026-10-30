using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Owns transient scanned geometry and the persistent state for the active dungeon.
/// </summary>
internal sealed class DungeonSession
{
    /// <summary>
    /// Identity, room bounds, and entrance metadata for this session.
    /// </summary>
    public DungeonArea Area;

    /// <summary>
    /// Persistent exploration, chest markers, and traversal evidence.
    /// </summary>
    public SavedRun Data;

    /// <summary>
    /// Mutable explored sample set reconstructed from persistent keys.
    /// </summary>
    public readonly Exploration Exploration = new();

    /// <summary>
    /// Chest identity and collection registry backed by this session's save data.
    /// </summary>
    public ChestRegistry Chests;

    /// <summary>
    /// Detected ascending floor elevations used for view selection.
    /// </summary>
    public List<float> Floors = new();

    /// <summary>
    /// Supported floor destinations and their architectural elevations.
    /// </summary>
    public FloorLayout Layout = new();

    /// <summary>
    /// Whether saving is disabled to protect corrupt or unsupported data.
    /// </summary>
    public bool ReadOnly;
}
