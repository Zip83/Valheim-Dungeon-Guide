using System;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Learns approved destructible prefab identities from actual local dungeon destruction.
/// </summary>
internal sealed class DestructionRecorder
{
    private readonly GuideSettings _settings;
    private readonly Func<DungeonArea> _area;
    private readonly Action _changed;

    /// <summary>
    /// Observes destruction within the active area and records approved prefabs through configuration.
    /// </summary>
    public DestructionRecorder(GuideSettings settings, Func<DungeonArea> area, Action changed)
    {
        _settings = settings;
        _area = area;
        _changed = changed;
    }

    /// <summary>
    /// Accepts actual local dungeon destruction as evidence for an approved breakable prefab.
    /// </summary>
    public void Observe(Destructible target)
    {
        var current = _area();
        if (current == null || !target || !current.Bounds.Contains(target.transform.position))
        {
            return;
        }

        _changed();
        if (target.GetComponentInParent<Piece>())
        {
            return;
        }

        var colliders = target.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).ToArray();
        if (colliders.Length == 0 || !colliders.All(c => Mathf.Min(c.bounds.size.x, c.bounds.size.z) < 1.2f))
        {
            return;
        }

        // The player actually destroyed this prefab. Future instances still need floor support
        // on both sides and clearance checks before they can be used as passages.
        var prefab = target.gameObject.name.Replace("(Clone)", "").Trim();
        _settings.RememberDestroyedPrefab(prefab);
    }
}
