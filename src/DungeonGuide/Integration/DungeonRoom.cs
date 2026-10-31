using UnityEngine;

namespace DungeonGuide;

/// <summary>Snapshots a generated room's oriented volume and stable local coordinate system.</summary>
internal sealed class DungeonRoom
{
    private readonly RoomVolume _volume;
    private readonly Matrix4x4 _localToWorld;

    /// <summary>Stable pose and prefab identity, scoped by the containing dungeon save.</summary>
    public string Id
    {
        get;
    }

    /// <summary>Freezes transforms on the game thread without retaining mutable Unity room objects.</summary>
    public DungeonRoom(Room room)
    {
        var inverse = room.transform.worldToLocalMatrix;
        _localToWorld = room.transform.localToWorldMatrix;
        _volume = new RoomVolume(UnityPoints.P(room.transform.position),
            new Point(inverse.m00, inverse.m01, inverse.m02),
            new Point(inverse.m10, inverse.m11, inverse.m12),
            new Point(inverse.m20, inverse.m21, inverse.m22), UnityPoints.P((Vector3)room.m_size * 0.5f));
        Id = room.name.Replace("(Clone)", "") + ":" + Point.Key(UnityPoints.P(room.transform.position))
            + ":" + Point.Key(UnityPoints.P(room.transform.eulerAngles));
    }

    /// <summary>Checks oriented room membership without the overlap tolerance used for dungeon detection.</summary>
    public bool Contains(Vector3 world) => _volume.Contains(UnityPoints.P(world));

    /// <summary>Converts entry positions into persistent room-local anchors.</summary>
    public Point Local(Vector3 world) => _volume.Local(UnityPoints.P(world));

    /// <summary>Restores the physical elevation of a saved room-local entry.</summary>
    public float Height(Point local) => _localToWorld.MultiplyPoint3x4(UnityPoints.V(local)).y;
}
