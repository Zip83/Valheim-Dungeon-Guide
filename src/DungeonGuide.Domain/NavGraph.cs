using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Stores sampled dungeon geometry as directed, policy-filtered navigation connections.
/// </summary>
public sealed class NavGraph
{
    /// <summary>
    /// Indexed standable floor samples that own their outgoing connections.
    /// </summary>
    public readonly List<NavNode> Nodes = new();

    /// <summary>
    /// Adds a passage without duplicate edges, optionally adding the reverse direction for traversable returns.
    /// </summary>
    /// <param name="a">Source graph-node index.</param>
    /// <param name="b">Destination graph-node index.</param>
    /// <param name="kind">Physical action required by the passage.</param>
    /// <param name="verified">Whether required geometry or observed traversal evidence has been established.</param>
    /// <param name="bidirectional">Whether to add the reverse connection as well as the forward connection.</param>
    /// <param name="obstacle">Associated obstacle identity, or null for an unobstructed connection.</param>
    public void Connect(
        int a,
        int b,
        PassageKind kind = PassageKind.Walk,
        bool verified = true,
        bool bidirectional = true,
        string obstacle = null)
    {
        Add(a, b, kind, verified, obstacle);
        if (bidirectional)
        {
            Add(b, a, kind, verified, obstacle);
        }
    }

    private void Add(int a, int b, PassageKind kind, bool verified, string obstacle)
    {
        if (a == b || Nodes[a].Edges.Exists(e => e.To == b))
        {
            return;
        }

        Nodes[a].Edges.Add(new NavEdge(b, kind, verified, obstacle));
    }

    /// <summary>
    /// Returns the closest sample within the three-dimensional distance limit, or -1 if no sample qualifies.
    /// </summary>
    /// <param name="p">World-space position measured in metres.</param>
    /// <param name="limit">Maximum three-dimensional search distance in world metres.</param>
    /// <returns>Nearest qualifying graph-node index, or -1 when none lies within the distance limit.</returns>
    public int Nearest(Point p, float limit)
    {
        var result = -1;
        for (var i = 0; i < Nodes.Count; i++)
        {
            var d = Point.Distance(p, Nodes[i].Position);
            if (d < limit)
            {
                result = i;
                limit = d;
            }
        }

        return result;
    }
}
