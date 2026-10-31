using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Controls optional traversal actions and penalties that favor ordinary routes.
/// </summary>
public sealed class RoutePolicy
{
    /// <summary>
    /// Whether approved destructive passages are allowed; disabled by default.
    /// </summary>
    public bool Breakables;

    /// <summary>
    /// Rejects unverified passages and optional actions that the player has not enabled.
    /// </summary>
    /// <param name="e">Directed passage being checked against route permissions or cost.</param>
    /// <returns>True when verification and optional traversal permissions permit this edge.</returns>
    public bool Allows(NavEdge e) => e.Verified && (e.Kind != PassageKind.Breakable || Breakables);

    /// <summary>
    /// Adds a cost for special traversal so an ordinary route is preferred when practical.
    /// </summary>
    /// <param name="e">Directed passage being checked against route permissions or cost.</param>
    /// <returns>Additional non-negative traversal cost used to favor ordinary routes.</returns>
    public float Penalty(NavEdge e) => e.Kind == PassageKind.Breakable
        ? 25
        : e.Kind == PassageKind.Jump || e.Kind == PassageKind.Drop ? 15 : e.Kind == PassageKind.Door ? 3 : 0;
}
