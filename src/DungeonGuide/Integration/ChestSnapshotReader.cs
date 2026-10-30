using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Reads locally available chest identity and contents without changing network ownership.
/// </summary>
internal sealed class ChestSnapshotReader
{
    /// <summary>
    /// Captures the chest identity, position, and any locally available inventory summary.
    /// </summary>
    public ChestSnapshot Read(Container chest, bool discovered)
    {
        var transformPosition = chest.transform.position;
        var p = transformPosition;
        var box = chest.GetComponentsInChildren<Collider>().FirstOrDefault(c => c.enabled && !c.isTrigger);
        if (box)
        {
            p = new Vector3(box.bounds.center.x, box.bounds.min.y, box.bounds.center.z);
        }

        var view = chest.GetComponent<ZNetView>();
        var zdo = view ? view.GetZDO() : null;
        var legacy = UnityPoints.PositionId(transformPosition);
        var id = zdo != null && !zdo.m_uid.Equals(ZDOID.None) ? "zdo_" + zdo.m_uid : legacy;
        string contents = null;
        var inventory = chest.GetInventory();
        if (inventory != null)
        {
            contents = string.Join(
                ", ",
                inventory.GetAllItems().Take(5)
                    .Select(i => i.m_stack + "× "
                        + (Localization.instance != null
                        ? Localization.instance.Localize(i.m_shared.m_name)
                        : i.m_shared.m_name)));
        }

        return new ChestSnapshot
        {
            Id = id,
            LegacyId = legacy,
            Position = UnityPoints.P(p),
            Contents = contents,
            Discovered = discovered
        };
    }
}
