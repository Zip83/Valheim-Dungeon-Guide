using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Stores persistent chest notes and collection state using legacy JSON field names.
/// </summary>
[Serializable]
public sealed class ChestRecord
{
    /// <summary>
    /// Stable network identifier, or the legacy position identifier until a network identity is observed.
    /// </summary>
    public string id;

    /// <summary>
    /// Player-defined label retained when the chest identity is upgraded.
    /// </summary>
    public string label;

    /// <summary>
    /// Last locally available inventory summary; unavailable snapshots do not erase it.
    /// </summary>
    public string contents;

    /// <summary>
    /// Last known world-space position used for floor assignment and routing.
    /// </summary>
    public Point position;

    /// <summary>
    /// Whether the player has marked this chest for a later collection trip.
    /// </summary>
    public bool collect;

    /// <summary>
    /// Whether the player explicitly confirmed collection; visiting does not set this flag.
    /// </summary>
    public bool done;

    /// <summary>
    /// Whether exploration or a manual marker has made this chest known to the player.
    /// </summary>
    public bool discovered;
}
