namespace DungeonGuide;

/// <summary>
/// Enumerates user actions accepted by the dungeon guide command dispatcher.
/// </summary>
internal enum GuideCommand
{
    /// <summary>
    /// Switch between native world and dungeon maps.
    /// </summary>
    ToggleWorld,
    /// <summary>
    /// Switch between explored and fully scanned floor geometry.
    /// </summary>
    ToggleReveal,
    /// <summary>
    /// Manually show or hide the standalone map.
    /// </summary>
    ToggleOverlay,
    /// <summary>
    /// Browse the next higher detected floor.
    /// </summary>
    FloorUp,
    /// <summary>
    /// Browse the next lower detected floor.
    /// </summary>
    FloorDown,
    /// <summary>
    /// Restore current-floor following and fit.
    /// </summary>
    Follow,
    /// <summary>
    /// Navigate to a real floor above the current physical height.
    /// </summary>
    NavigateUp,
    /// <summary>
    /// Navigate to a real floor below the current physical height.
    /// </summary>
    NavigateDown,
    /// <summary>
    /// Navigate toward accessible unexplored geometry.
    /// </summary>
    Explore,
    /// <summary>
    /// Navigate to the known entrance or labeled scan origin.
    /// </summary>
    Entrance,
    /// <summary>
    /// Stop the active navigation request.
    /// </summary>
    Stop,
    /// <summary>
    /// Rebuild loaded dungeon geometry.
    /// </summary>
    Rescan,
    /// <summary>
    /// Enable or disable approved destructive passages.
    /// </summary>
    ToggleBreakables,
    /// <summary>
    /// Enable or disable verified jumps and directional drops.
    /// </summary>
    ToggleJumps,
    /// <summary>
    /// Mark the actual interactive chest for collection.
    /// </summary>
    MarkChest,
    /// <summary>
    /// Edit a name or note for the actual interactive chest.
    /// </summary>
    NameChest,
    /// <summary>
    /// Confirm collection at the actual interactive chest.
    /// </summary>
    CollectChest,
    /// <summary>
    /// Cycle known chest navigation destinations.
    /// </summary>
    NextChest
}
