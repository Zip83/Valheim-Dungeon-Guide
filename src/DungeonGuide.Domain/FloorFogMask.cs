using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Reconstructs inward-feathered fog from saved explored samples without revealing unknown geometry.
/// </summary>
internal sealed class FloorFogMask
{
    /// <summary>Allocates an unpublished fog mask for one completed floor raster.</summary>
    public FloorFogMask(FloorPlanRaster raster)
    {
        Raster = raster;
        Opacity = new byte[raster.Size * raster.Size];
    }

    /// <summary>Projection and sample ownership to which this mask belongs.</summary>
    public FloorPlanRaster Raster
    {
        get;
    }

    /// <summary>Fog alpha: 255 completely hides unknown space, while 0 exposes explored interior.</summary>
    public byte[] Opacity
    {
        get;
    }

    /// <summary>Builds a mask cooperatively; its exploration input is read-only.</summary>
    public IEnumerable<byte> Build(Exploration exploration)
    {
        var feather = Math.Max(1, Math.Min(8, (int)Math.Ceiling(0.4f / Raster.Span * Raster.Size)));
        var distance = new byte[Opacity.Length];
        foreach (var step in MeasureExploredInterior(exploration, distance, feather))
        {
            yield return step;
        }

        foreach (var step in ApplyOpacity(distance, feather))
        {
            yield return step;
        }
    }

    /// <summary>Reports whether a world position lies within already explored coverage on this floor.</summary>
    public bool Visible(Point point)
    {
        var x = (int)Math.Floor(Raster.X(point.x));
        var y = (int)Math.Floor(Raster.Y(point.z));
        return x >= 0 && y >= 0 && x < Raster.Size && y < Raster.Size
            && Opacity[y * Raster.Size + x] < 255;
    }

    private IEnumerable<byte> MeasureExploredInterior(Exploration exploration, byte[] distance, int feather)
    {
        var size = Raster.Size;
        for (var y = 0; y < size; y++)
        {
            yield return 0;
            for (var x = 0; x < size; x++)
            {
                var i = y * size + x;
                var owner = Raster.Owners[i] - 1;
                if (owner < 0 || !exploration.Has(Raster.Graph.Nodes[owner]))
                {
                    continue;
                }

                var left = x > 0 ? distance[i - 1] : 0;
                var down = y > 0 ? distance[i - size] : 0;
                distance[i] = (byte)Math.Min(feather, Math.Min(left, down) + 1);
            }
        }
    }

    private IEnumerable<byte> ApplyOpacity(byte[] distance, int feather)
    {
        var size = Raster.Size;
        for (var y = size - 1; y >= 0; y--)
        {
            yield return 0;
            for (var x = size - 1; x >= 0; x--)
            {
                var i = y * size + x;
                if (distance[i] > 0)
                {
                    var right = x + 1 < size ? distance[i + 1] : 0;
                    var up = y + 1 < size ? distance[i + size] : 0;
                    distance[i] = (byte)Math.Min(distance[i], Math.Min(right, up) + 1);
                }

                Opacity[i] = (byte)(255 - distance[i] * 255 / feather);
            }
        }
    }
}
