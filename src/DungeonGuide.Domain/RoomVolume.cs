using System;

namespace DungeonGuide;

/// <summary>Tests an oriented room volume using frozen world-to-local basis vectors.</summary>
internal sealed class RoomVolume
{
    private readonly Point _origin;
    private readonly Point _x;
    private readonly Point _y;
    private readonly Point _z;
    private readonly Point _half;

    /// <summary>Accepts the world origin, inverse-transform rows and local half-extents, including scale.</summary>
    public RoomVolume(Point origin, Point x, Point y, Point z, Point half)
    {
        _origin = origin;
        _x = x;
        _y = y;
        _z = z;
        _half = half;
    }

    /// <summary>Converts a world position to the stable coordinate system used by persisted entry anchors.</summary>
    public Point Local(Point world)
    {
        var delta = new Point(world.x - _origin.x, world.y - _origin.y, world.z - _origin.z);
        return new Point(Dot(delta, _x), Dot(delta, _y), Dot(delta, _z));
    }

    /// <summary>Rejects points outside the actual rotated room, even if they lie in its world AABB.</summary>
    public bool Contains(Point world)
    {
        var local = Local(world);
        return Math.Abs(local.x) <= _half.x && Math.Abs(local.y) <= _half.y && Math.Abs(local.z) <= _half.z;
    }

    private static float Dot(Point a, Point b) => a.x * b.x + a.y * b.y + a.z * b.z;
}
