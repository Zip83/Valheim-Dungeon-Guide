using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Stamps a cached, antialiased player pointer with an outline and a lightly shaded fill.</summary>
internal sealed class PlayerMarkerRenderer
{
    private readonly Action<int, int, Color32> _setPixel;
    private readonly List<(int X, int Y, Color32 Color)> _glyph = new(512);
    private float _yaw = float.NaN;
    private float _sin;
    private float _cos;

    /// <summary>Uses the map marker layer's pixel sink so clipping and dirty uploads remain shared.</summary>
    public PlayerMarkerRenderer(Action<int, int, Color32> setPixel)
    {
        _setPixel = setPixel;
    }

    /// <summary>Centers the pointer on the player and rotates its tip toward the supplied world heading.</summary>
    public void Draw(int x, int y, float yawDegrees)
    {
        EnsureGlyph(yawDegrees);
        foreach (var pixel in _glyph)
        {
            _setPixel(x + pixel.X, y + pixel.Y, pixel.Color);
        }
    }

    private void EnsureGlyph(float yaw)
    {
        if (!float.IsNaN(_yaw) && Mathf.Abs(Mathf.DeltaAngle(_yaw, yaw)) < PlayerMarkerStyle.AngleStep)
        {
            return;
        }

        _yaw = yaw;
        _sin = Mathf.Sin(yaw * Mathf.Deg2Rad);
        _cos = Mathf.Cos(yaw * Mathf.Deg2Rad);
        _glyph.Clear();
        for (var y = -PlayerMarkerStyle.Radius; y <= PlayerMarkerStyle.Radius; y++)
        {
            RasterizeRow(y);
        }
    }

    private void RasterizeRow(int y)
    {
        for (var x = -PlayerMarkerStyle.Radius; x <= PlayerMarkerStyle.Radius; x++)
        {
            var color = Pixel(x, y);
            if (color.a != 0)
            {
                _glyph.Add((x, y, color));
            }
        }
    }

    private Color32 Pixel(int x, int y)
    {
        var right = x * _cos - y * _sin;
        var forward = x * _sin + y * _cos;
        var outer = Coverage(right, forward, PlayerMarkerStyle.OutlineScale);
        if (outer == 0)
        {
            return default;
        }

        var inner = Coverage(right, forward, 1);
        var fill = Color32.Lerp(PlayerMarkerStyle.TailTint, MapPalette.Player,
            Mathf.InverseLerp(PlayerMarkerStyle.Tail, PlayerMarkerStyle.Tip, forward));
        var color = Color32.Lerp(PlayerMarkerStyle.Outline, fill, inner / (float)outer);
        color.a = (byte)(outer * 255 / 4);
        return color;
    }

    private int Coverage(float right, float forward, float scale)
    {
        var samples = 0;
        for (var x = -1; x <= 1; x += 2)
        {
            for (var y = -1; y <= 1; y += 2)
            {
                var sampleRight = right + (x * _cos - y * _sin) * 0.25f;
                var sampleForward = forward + (x * _sin + y * _cos) * 0.25f;
                samples += Contains(sampleRight / scale, sampleForward / scale) ? 1 : 0;
            }
        }

        return samples;
    }

    private static bool Contains(float right, float forward)
    {
        var side = Mathf.Abs(right) / PlayerMarkerStyle.HalfWidth;
        var top = PlayerMarkerStyle.Tip - side * (PlayerMarkerStyle.Tip - PlayerMarkerStyle.Tail);
        var bottom = PlayerMarkerStyle.Notch - side * (PlayerMarkerStyle.Notch - PlayerMarkerStyle.Tail);
        return forward >= bottom && forward <= top;
    }
}
