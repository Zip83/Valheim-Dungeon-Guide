using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Revalidates only recorded traversals through a prebuilt edge index instead of scanning the entire graph.
/// </summary>
internal sealed class TraversalProofVerifier
{
    private readonly List<NavEdge> _verified = new();

    /// <summary>
    /// Revokes previously accepted evidence, including edges from a graph that has just been replaced.
    /// </summary>
    public void Reset()
    {
        foreach (var edge in _verified)
        {
            edge.Verified = false;
        }

        _verified.Clear();
    }

    /// <summary>
    /// Applies current capabilities to recorded directional proofs, revoking removed or no-longer-valid evidence.
    /// </summary>
    public void Apply(
        IReadOnlyDictionary<(string, string), NavEdge> candidates,
        IReadOnlyList<TraversalProof> proofs,
        float jumpForce,
        float jumpSkill,
        float stamina,
        float jumpStaminaUsage)
    {
        Reset();
        foreach (var proof in proofs)
        {
            if (!candidates.TryGetValue((Point.Key(proof.from), Point.Key(proof.to)), out var edge))
            {
                continue;
            }

            edge.Verified = proof.jumpForce <= jumpForce + 0.01f
                && proof.jumpSkill <= jumpSkill + 0.01f
                && (edge.Kind == PassageKind.Drop || stamina >= jumpStaminaUsage);
            if (edge.Verified)
            {
                _verified.Add(edge);
            }
        }
    }
}
