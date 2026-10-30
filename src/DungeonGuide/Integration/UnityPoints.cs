using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Converts Unity vectors to pure world positions and stable coordinate identifiers.
/// </summary>
internal static class UnityPoints
{
    /// <summary>
    /// Copies a Unity vector into a pure world-space position.
    /// </summary>
    public static Point P(Vector3 v) => new(v.x, v.y, v.z);

    /// <summary>
    /// Copies a pure world-space position into a Unity vector.
    /// </summary>
    public static Vector3 V(Point p) => new(p.x, p.y, p.z);

    /// <summary>
    /// Formats a stable coordinate-based fallback identifier for a Unity position.
    /// </summary>
    public static string PositionId(Vector3 v) =>
        Mathf.RoundToInt(v.x * 10) + "_" + Mathf.RoundToInt(v.y * 10) + "_" + Mathf.RoundToInt(v.z * 10);
}
