using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Requires verified traversal and applies penalties that favor ordinary routes.
/// </summary>
public sealed class RoutePolicy
{
    /// <summary>
    /// Rejects every unverified passage; verified actions need no manual opt-in.
    /// </summary>
    /// <param name="e">Directed passage being checked against route permissions or cost.</param>
    /// <returns>True when current verification permits this edge.</returns>
    public bool Allows(NavEdge e) => e.Verified;

    /// <summary>
    /// Adds a cost for special traversal so an ordinary route is preferred when practical.
    /// </summary>
    /// <param name="e">Directed passage being checked against route permissions or cost.</param>
    /// <returns>Additional non-negative traversal cost used to favor ordinary routes.</returns>
    public float Penalty(NavEdge e) => e.Kind == PassageKind.Breakable
        ? 25
        : e.Kind == PassageKind.Jump || e.Kind == PassageKind.Drop ? 15 : e.Kind == PassageKind.Door ? 3 : 0;
}
