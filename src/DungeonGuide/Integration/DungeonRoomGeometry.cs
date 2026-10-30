using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Converts loaded room geometry into containment checks and world-space scan bounds.
/// </summary>
internal static class DungeonRoomGeometry
{
    /// <summary>
    /// Tests room-local extents with the existing half-metre membership tolerance.
    /// </summary>
    /// <param name="room">Loaded room providing its local size and transform.</param>
    /// <param name="position">World-space point to test.</param>
    /// <returns>True when the point lies within the room-local extents and membership tolerance.</returns>
    public static bool Contains(Room room, Vector3 position)
    {
        var local = room.transform.InverseTransformPoint(position);
        var halfSize = (Vector3)room.m_size * 0.5f;
        const float Tolerance = 0.5f;

        return Mathf.Abs(local.x) <= halfSize.x + Tolerance
            && Mathf.Abs(local.y) <= halfSize.y + Tolerance
            && Mathf.Abs(local.z) <= halfSize.z + Tolerance;
    }

    /// <summary>
    /// Encloses all transformed corners, including rotated rooms.
    /// </summary>
    /// <param name="room">Loaded room whose eight corners define its extent.</param>
    /// <returns>Axis-aligned world bounds enclosing the transformed room corners.</returns>
    public static Bounds WorldBounds(Room room)
    {
        var size = room.m_size;
        var bounds = new Bounds(room.transform.position, Vector3.zero);
        for (var x = -1; x <= 1; x += 2)
        {
            for (var y = -1; y <= 1; y += 2)
            {
                for (var z = -1; z <= 1; z += 2)
                {
                    var corner = new Vector3(size.x * x, size.y * y, size.z * z) * 0.5f;
                    bounds.Encapsulate(room.transform.TransformPoint(corner));
                }
            }
        }

        return bounds;
    }

    /// <summary>
    /// Encloses a non-empty room-bounds collection with the existing six-metre scan padding.
    /// </summary>
    /// <param name="rooms">Non-empty collection of world-space room bounds.</param>
    /// <returns>Combined bounds expanded by the existing scan padding.</returns>
    public static Bounds Enclose(Bounds[] rooms)
    {
        var bounds = rooms[0];
        foreach (var room in rooms)
        {
            bounds.Encapsulate(room);
        }

        bounds.Expand(6);
        return bounds;
    }
}
