using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Identifies the kind of destination currently requested by the player.
/// </summary>
internal enum NavigationGoal
{
    /// <summary>
    /// Navigation is inactive.
    /// </summary>
    None,
    /// <summary>
    /// Find an accessible unexplored floor sample.
    /// </summary>
    Explore,
    /// <summary>
    /// Return to the selected chest.
    /// </summary>
    Chest,
    /// <summary>
    /// Return to the known entrance or labeled scan origin.
    /// </summary>
    Entrance,
    /// <summary>
    /// Reach a remembered higher or lower architectural level.
    /// </summary>
    Floor
}
