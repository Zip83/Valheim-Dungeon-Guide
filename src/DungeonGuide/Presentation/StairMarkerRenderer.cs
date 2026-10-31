using System;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Draws thick, outlined stair arrows through the marker layer's shared clipped pixel brush.</summary>
internal sealed class StairMarkerRenderer
{
    private const int HalfWidth = 5;
    private const int PairOffset = 8;
    private readonly Action<int, int, int, Color32> _brush;

    /// <summary>Reuses the map's pixel brush so fog clipping and dirty uploads remain authoritative.</summary>
    public StairMarkerRenderer(Action<int, int, int, Color32> brush)
    {
        _brush = brush;
    }

    /// <summary>Separates simultaneous up/down arrows without hiding either direction at a landing.</summary>
    public void Draw(int x, int y, bool up, bool down)
    {
        if (up)
        {
            Arrow(x - (down ? PairOffset : 0), y, 1);
        }

        if (down)
        {
            Arrow(x + (up ? PairOffset : 0), y, -1);
        }
    }

    private void Arrow(int x, int y, int direction)
    {
        Stroke(x, y, direction, 2, MapPalette.FloorTransitionOutline);
        Stroke(x, y, direction, 1, MapPalette.FloorTransition);
    }

    private void Stroke(int x, int y, int direction, int radius, Color32 color)
    {
        Head(x, y, direction, radius, color);
        Stem(x, y, direction, radius, color);
    }

    private void Head(int x, int y, int direction, int radius, Color32 color)
    {
        for (var dx = -HalfWidth; dx <= HalfWidth; dx++)
        {
            _brush(x + dx, y + direction * (HalfWidth - Math.Abs(dx)), radius, color);
        }
    }

    private void Stem(int x, int y, int direction, int radius, Color32 color)
    {
        for (var step = -4; step < HalfWidth; step++)
        {
            _brush(x, y + direction * step, radius, color);
        }
    }
}
