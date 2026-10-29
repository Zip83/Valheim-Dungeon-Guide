using System;

namespace DungeonGuide;

/// <summary>
/// Stores a world-space position in metres without depending on Unity.
/// </summary>
[Serializable]
public struct Point
{
    /// <summary>
    /// World-space horizontal X coordinate; the lowercase name is part of the save format.
    /// </summary>
    public float x;

    /// <summary>
    /// World-space elevation; the lowercase name is part of the save format.
    /// </summary>
    public float y;

    /// <summary>
    /// World-space horizontal Z coordinate; the lowercase name is part of the save format.
    /// </summary>
    public float z;

    /// <summary>
    /// Creates a world-space position from coordinates measured in metres.
    /// </summary>
    /// <param name="x">World-space horizontal X coordinate in metres.</param>
    /// <param name="y">World-space elevation in metres.</param>
    /// <param name="z">Horizontal Z coordinate in world metres.</param>
    public Point(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    /// <summary>
    /// Calculates the three-dimensional Euclidean distance between two positions in metres.
    /// </summary>
    /// <param name="a">First world-space endpoint.</param>
    /// <param name="b">Second world-space endpoint.</param>
    /// <returns>Three-dimensional distance in world metres.</returns>
    public static float Distance(Point a, Point b)
    {
        var x = a.x - b.x;
        var y = a.y - b.y;
        var z = a.z - b.z;
        return (float)Math.Sqrt(x * x + y * y + z * z);
    }

    /// <summary>
    /// Quantizes a position to a stable exploration key using 0.05 m horizontal and 0.5 m vertical cells.
    /// </summary>
    /// <param name="p">World-space position measured in metres.</param>
    /// <returns>Stable quantized sample key used by exploration persistence.</returns>
    public static string Key(Point p) =>
        (int)Math.Round(p.x * 20) + "_" + (int)Math.Round(p.y * 2) + "_" + (int)Math.Round(p.z * 20);
}
