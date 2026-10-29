using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Describes a directed connection and the evidence required before navigation may use it.
/// </summary>
public sealed class NavEdge
{
    /// <summary>
    /// Destination node index in the owning graph.
    /// </summary>
    public int To;

    /// <summary>
    /// Required traversal action, such as walking, opening a door, or a verified jump.
    /// </summary>
    public PassageKind Kind;

    /// <summary>
    /// Whether the passage has the required geometric or observed traversal evidence.
    /// </summary>
    public bool Verified;

    /// <summary>
    /// Associated obstacle identity when interaction may invalidate this passage.
    /// </summary>
    public string Obstacle;

    /// <summary>
    /// Records a directed passage and its verification state without inventing a reverse connection.
    /// </summary>
    /// <param name="to">Destination graph-node index.</param>
    /// <param name="kind">Physical action required by the passage.</param>
    /// <param name="verified">Whether required geometry or observed traversal evidence has been established.</param>
    /// <param name="obstacle">Associated obstacle identity, or null for an unobstructed connection.</param>
    public NavEdge(int to, PassageKind kind, bool verified, string obstacle = null)
    {
        To = to;
        Kind = kind;
        Verified = verified;
        Obstacle = obstacle;
    }
}
