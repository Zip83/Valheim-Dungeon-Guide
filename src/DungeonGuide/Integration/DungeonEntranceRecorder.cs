using System;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Retains a confirmed local dungeon arrival without relying on sampled floor geometry.</summary>
internal sealed class DungeonEntranceRecorder
{
    private Point _doorway;
    private Point _arrival;
    private bool _observed;
    private long _world;

    /// <summary>Requests prompt session detection after an actual local dungeon transfer.</summary>
    public event Action Observed;

    /// <summary>Records only successful local-player transfers from outdoors to a linked interior doorway.</summary>
    public void Observe(Teleport source, Humanoid character)
    {
        var target = source ? source.m_targetPoint : null;
        if (!target || !Player.m_localPlayer || character != Player.m_localPlayer || !ZNet.instance
            || Character.InInterior(source.transform.position) || !Character.InInterior(target.transform.position))
        {
            return;
        }

        var doorway = target.transform;
        _doorway = UnityPoints.P(doorway.position);
        // Freeze the native GetTeleportPoint result before doorway objects can unload.
        _arrival = UnityPoints.P(doorway.position + doorway.forward - doorway.up);
        _observed = true;
        _world = ZNet.instance.GetWorldUID();
        Observed?.Invoke();
    }

    /// <summary>Returns the native arrival point only for the same world and containing generated room.</summary>
    public Point? Find(Room[] rooms)
    {
        if (!_observed || !ZNet.instance || _world != ZNet.instance.GetWorldUID()
            || !rooms.Any(room => DungeonRoomGeometry.Contains(room, UnityPoints.V(_doorway))
                || DungeonRoomGeometry.Contains(room, UnityPoints.V(_arrival))))
        {
            return null;
        }

        return _arrival;
    }
}
