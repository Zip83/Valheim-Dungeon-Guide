using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Recognizes thin destructible barriers and delegates damage feasibility to the native game rules.
/// </summary>
internal sealed class BreakablePassageVerifier
{
    private readonly DestructibleDamageProbe _damage;

    /// <summary>
    /// Uses a read-only equipment and resistance probe rather than a prefab approval list.
    /// </summary>
    public BreakablePassageVerifier(DestructibleDamageProbe damage)
    {
        _damage = damage;
    }

    /// <summary>
    /// Identifies one thin barrier and reports whether current carried equipment can damage it.
    /// </summary>
    public bool Matches(IReadOnlyList<Collider> blockers, out string prefab, out bool verified)
    {
        prefab = null;
        verified = false;
        if (blockers.Count == 0)
        {
            return false;
        }

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
        verified = _damage.CanBreak(d);
        return true;
    }
}
