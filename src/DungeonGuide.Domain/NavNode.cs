using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Represents one sampled standable position and its directed navigation connections.
/// </summary>
public sealed class NavNode
{
    /// <summary>
    /// World-space location of the sampled floor point.
    /// </summary>
    public Point Position;

    /// <summary>
    /// Quantized position key shared with persistent exploration.
    /// </summary>
    public string Key
    {
        get;
    }

    /// <summary>
    /// Whether the local collision normal qualifies as a nearly horizontal floor.
    /// </summary>
    public bool Flat;

    /// <summary>
    /// Index assigned by architectural floor detection; it is not proof of a valid landing.
    /// </summary>
    public int Floor;

    /// <summary>
    /// Outgoing passages to other sampled nodes.
    /// </summary>
    public readonly List<NavEdge> Edges = new();

    /// <summary>
    /// Creates a sampled floor node and its stable exploration key.
    /// </summary>
    /// <param name="p">World-space position measured in metres.</param>
    /// <param name="flat">Whether the sampled surface qualifies as nearly horizontal.</param>
    public NavNode(Point p, bool flat = true)
    {
        Position = p;
        Key = Point.Key(p);
        Flat = flat;
    }
}
