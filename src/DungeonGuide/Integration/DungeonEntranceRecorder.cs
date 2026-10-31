using System;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Retains a confirmed local dungeon arrival without relying on sampled floor geometry.</summary>
internal sealed class DungeonEntranceRecorder
{
    private Teleport _entrance;
    private long _world;

    /// <summary>Records only successful local-player transfers from outdoors to a linked interior doorway.</summary>
    public void Observe(Teleport source, Humanoid character)
    {
        var target = source ? source.m_targetPoint : null;
        if (!target || character != Player.m_localPlayer || !ZNet.instance
            || Character.InInterior(source.transform.position) || !Character.InInterior(target.transform.position))
        {
            return;
        }

        _entrance = target;
        _world = ZNet.instance.GetWorldUID();
    }

    /// <summary>Returns the native arrival point only for the same world and containing generated room.</summary>
    public Point? Find(Room[] rooms)
    {
        if (!_entrance || !ZNet.instance || _world != ZNet.instance.GetWorldUID()
            || !rooms.Any(room => DungeonRoomGeometry.Contains(room, _entrance.transform.position)))
        {
            return null;
        }

        // Teleport.GetTeleportPoint uses the doorway's forward offset and one metre below its origin.
        var doorway = _entrance.transform;
        return UnityPoints.P(doorway.position + doorway.forward - doorway.up);
    }
}
