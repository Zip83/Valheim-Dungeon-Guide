using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Checks thin, supported destructible passages against explicitly approved prefab identities.
/// </summary>
internal sealed class BreakablePassageVerifier
{
    private readonly Func<string, bool> _approved;

    /// <summary>
    /// Uses the configured prefab approval predicate to validate destructible blockers.
    /// </summary>
    public BreakablePassageVerifier(Func<string, bool> approved)
    {
        _approved = approved;
    }

    /// <summary>
    /// Identifies a thin supported destructible obstacle and reports whether its prefab is approved.
    /// </summary>
    public bool Matches(IReadOnlyList<Collider> blockers, out string prefab, out bool verified)
    {
        prefab = null;
        verified = false;
        var d = blockers[0].GetComponentInParent<Destructible>();
        if (!d || d.GetComponentInParent<Piece>())
        {
            return false;
        }

        foreach (var collider in blockers)
        {
            if (collider.GetComponentInParent<Destructible>() != d
                || !(Mathf.Min(collider.bounds.size.x, collider.bounds.size.z) < 1.2f))
            {
                return false;
            }
        }

        prefab = d.gameObject.name.Replace("(Clone)", "").Trim();
        verified = d.m_health > 0 && _approved(prefab);
        return true;
    }
}
