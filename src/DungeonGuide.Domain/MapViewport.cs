using System;

namespace DungeonGuide;

/// <summary>
/// Stores normalized coordinates for a square region of the dungeon texture.
/// </summary>
public struct MapViewport
{
    /// <summary>
    /// Horizontal texture coordinate of the viewport origin.
    /// </summary>
    public float X;

    /// <summary>
    /// Vertical texture coordinate of the viewport origin.
    /// </summary>
    public float Y;

    /// <summary>
    /// Normalized width and height of the square viewport.
    /// </summary>
    public float Size;

    /// <summary>
    /// Creates a square viewport from its normalized origin and side length.
    /// </summary>
    /// <param name="x">Normalized X coordinate of the viewport origin.</param>
    /// <param name="y">Normalized Y coordinate of the viewport origin.</param>
    /// <param name="size">Normalized side length of the square map viewport.</param>
    public MapViewport(float x, float y, float size)
    {
        X = x;
        Y = y;
        Size = size;
    }
}
