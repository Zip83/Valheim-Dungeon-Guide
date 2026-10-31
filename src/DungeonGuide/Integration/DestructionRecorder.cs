using System;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Classifies observed destruction as a passage refresh or a structural geometry invalidation.</summary>
internal sealed class DestructionRecorder
{
    private readonly Func<DungeonSession> _session;
    private readonly Action _geometryChanged;
    private readonly Action _permissionsChanged;

    /// <summary>Observes the active dungeon without learning prefab lists or modifying game objects.</summary>
    public DestructionRecorder(Func<DungeonSession> session, Action geometryChanged, Action permissionsChanged)
    {
        _session = session;
        _geometryChanged = geometryChanged;
        _permissionsChanged = permissionsChanged;
    }

    /// <summary>Refreshes known barriers cheaply; unknown or floor-supporting destruction requires automatic resampling.</summary>
    public void Observe(Destructible target)
    {
        var current = _session();
        if (current == null || !target || !current.Area.Bounds.Contains(target.transform.position)
            || target.GetComponentInParent<Container>() || target.GetComponentInParent<Character>())
        {
            return;
        }

        if (IsPassageOnly(current.Scan, target))
        {
            _permissionsChanged();
        }
        else
        {
            _geometryChanged();
        }
    }

    private static bool IsPassageOnly(ScanResult scan, Destructible target)
    {
        var prefab = target.gameObject.name.Replace("(Clone)", "").Trim();
        if (scan == null || !scan.SpecialPassages.BreakablePrefabs.Contains(prefab)
            || target.GetComponentInParent<Piece>())
        {
            return false;
        }

        var colliders = target.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger);
        foreach (var collider in colliders)
        {
            if (SupportsSamples(scan, collider.bounds))
            {
                return false;
            }
        }

        return true;
    }

    private static bool SupportsSamples(ScanResult scan, Bounds bounds)
    {
        bounds.Expand(new Vector3(0.05f, 0.5f, 0.05f));
        var minX = Mathf.FloorToInt(bounds.min.x / scan.Spacing);
        var maxX = Mathf.CeilToInt(bounds.max.x / scan.Spacing);
        var minZ = Mathf.FloorToInt(bounds.min.z / scan.Spacing);
        var maxZ = Mathf.CeilToInt(bounds.max.z / scan.Spacing);
        if ((long)(maxX - minX + 1) * (maxZ - minZ + 1) > 1024)
        {
            return true;
        }

        for (var x = minX; x <= maxX; x++)
        {
            for (var z = minZ; z <= maxZ; z++)
            {
                if (scan.Columns.TryGetValue((x, z), out var nodes)
                    && nodes.Any(index => bounds.Contains(UnityPoints.V(scan.Graph.Nodes[index].Position))))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
