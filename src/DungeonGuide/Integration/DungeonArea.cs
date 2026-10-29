using System;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Describes loaded dungeon bounds, rooms, and an optional generator-provided entrance.
/// </summary>
internal sealed class DungeonArea
{
    /// <summary>
    /// Stable identity used to separate this dungeon's persistent state.
    /// </summary>
    public string Id;

    /// <summary>
    /// Dungeon name displayed in map and navigation presentation.
    /// </summary>
    public string Label;

    /// <summary>
    /// World-space bounds limiting collision sampling.
    /// </summary>
    public Bounds Bounds;

    /// <summary>
    /// Loaded generator room bounds used to constrain dungeon sampling.
    /// </summary>
    public Bounds[] Rooms;

    /// <summary>
    /// Known entrance position, or null when the generator cannot identify it.
    /// </summary>
    public Point? Entrance;
}
