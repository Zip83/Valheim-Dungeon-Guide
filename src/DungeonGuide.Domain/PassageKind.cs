using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Identifies the physical action required to traverse a navigation edge.
/// </summary>
public enum PassageKind
{
    /// <summary>
    /// Ordinary connected floor traversal.
    /// </summary>
    Walk,
    /// <summary>
    /// Supported traversal along stairs or a sloping connector.
    /// </summary>
    Stairs,
    /// <summary>
    /// Passage through a door requiring interaction and any applicable key.
    /// </summary>
    Door,
    /// <summary>
    /// Passage through a geometrically checked, approved destructible obstacle.
    /// </summary>
    Breakable,
    /// <summary>
    /// Directional jump supported by a recorded successful traversal.
    /// </summary>
    Jump,
    /// <summary>
    /// Directional drop that does not imply a climbable return route.
    /// </summary>
    Drop
}
