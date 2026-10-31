using System.Collections.Generic;
using System.Linq;

namespace DungeonGuide;

/// <summary>Binds successful directional traversal evidence without requiring an automatic gap candidate.</summary>
internal static class ObservedTraversalBinding
{
    /// <summary>Registers one previously observed transition after its current geometry has been checked.</summary>
    /// <param name="graph">Published graph containing the checked endpoints.</param>
    /// <param name="candidates">Index receiving the directed jump or drop for capability verification.</param>
    /// <param name="proof">Successful traversal evidence, normalized to the matching sample positions.</param>
    /// <param name="from">Checked takeoff node index.</param>
    /// <param name="to">Checked landing node index.</param>
    /// <returns>True when a directed airborne edge can receive this proof.</returns>
    public static bool Bind(NavGraph graph, IDictionary<(string, string), NavEdge> candidates,
        TraversalProof proof, int from, int to)
    {
        if (from < 0 || to < 0 || from >= graph.Nodes.Count || to >= graph.Nodes.Count || from == to)
        {
            return false;
        }

        var a = graph.Nodes[from];
        var b = graph.Nodes[to];
        proof.from = a.Position;
        proof.to = b.Position;
        var edge = a.Edges.FirstOrDefault(e => e.To == to);
        if (edge == null)
        {
            var kind = b.Position.y < a.Position.y - 0.6f ? PassageKind.Drop : PassageKind.Jump;
            graph.Connect(from, to, kind, false, false);
            edge = a.Edges.First(e => e.To == to);
        }

        if (edge.Kind != PassageKind.Jump && edge.Kind != PassageKind.Drop)
        {
            return false;
        }

        edge.Verified = false;
        candidates[(a.Key, b.Key)] = edge;
        return true;
    }
}
