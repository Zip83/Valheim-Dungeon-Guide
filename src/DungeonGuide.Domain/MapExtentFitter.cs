using System;

namespace DungeonGuide;

/// <summary>
/// Fits a square map viewport around one detected floor without revealing hidden geometry.
/// </summary>
public sealed class MapExtentFitter
{
    /// <summary>
    /// Returns normalized viewport bounds containing the selected floor with margin, or the full map when no extent is
    /// available.
    /// </summary>
    /// <param name="graph">Sampled floor graph whose node indices are used by the operation.</param>
    /// <param name="floor">Zero-based index into the detected floor elevations.</param>
    /// <param name="center">World-space centre represented by the full map texture.</param>
    /// <param name="span">World-space side length represented by the full map texture.</param>
    /// <returns>Normalized square viewport with margin; missing geometry falls back to the full map.</returns>
    public MapViewport Fit(NavGraph graph, int floor, Point center, float span)
    {
        if (span <= 0)
        {
            return new MapViewport(0, 0, 1);
        }

        var minX = float.MaxValue;
        var maxX = float.MinValue;
        var minZ = float.MaxValue;
        var maxZ = float.MinValue;
        foreach (var node in graph.Nodes)
        {
            if (node.Floor != floor)
            {
                continue;
            }

            minX = Math.Min(minX, node.Position.x);
            maxX = Math.Max(maxX, node.Position.x);
            minZ = Math.Min(minZ, node.Position.z);
            maxZ = Math.Max(maxZ, node.Position.z);
        }

        if (minX == float.MaxValue)
        {
            return new MapViewport(0, 0, 1);
        }

        var size = Math.Max(0.12f, Math.Min(1, (Math.Max(maxX - minX, maxZ - minZ) + 4) / span));
        var x = ((minX + maxX) * 0.5f - center.x) / span + 0.5f - size * 0.5f;
        var y = ((minZ + maxZ) * 0.5f - center.z) / span + 0.5f - size * 0.5f;
        return new MapViewport(Math.Max(0, Math.Min(1 - size, x)), Math.Max(0, Math.Min(1 - size, y)), size);
    }
}
