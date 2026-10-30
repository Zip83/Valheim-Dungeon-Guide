using System;

namespace DungeonGuide;

/// <summary>
/// Resolves vertical navigation goals against physical elevation and supported landing geometry.
/// </summary>
public sealed class FloorNavigationPolicy
{
    /// <summary>
    /// Finds the next higher or lower level from physical height, returning -1 at the dungeon limits.
    /// </summary>
    /// <param name="layout">Detected architectural elevations and supported landing nodes.</param>
    /// <param name="sourceHeight">Player elevation in world metres, independent of any nearest-floor label.</param>
    /// <param name="direction">Positive to select a higher floor; negative to select a lower floor.</param>
    /// <returns>Adjacent physical floor index, or -1 when no such level exists.</returns>
    public int Next(FloorLayout layout, float sourceHeight, int direction)
    {
        if (direction > 0)
        {
            for (var i = 0; i < layout.Levels.Count; i++)
            {
                if (layout.Levels[i] > sourceHeight + 0.75f)
                {
                    return i;
                }
            }
        }
        else
        {
            for (var i = layout.Levels.Count - 1; i >= 0; i--)
            {
                if (layout.Levels[i] < sourceHeight - 0.75f)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    /// <summary>
    /// Accepts only a broad, flat landing at the requested floor height, not a nearest-height label alone.
    /// </summary>
    /// <param name="graph">Sampled floor graph whose node indices are used by the operation.</param>
    /// <param name="layout">Detected architectural elevations and supported landing nodes.</param>
    /// <param name="node">Sampled floor node whose position and stable key identify exploration.</param>
    /// <param name="floor">Zero-based index into the detected floor elevations.</param>
    /// <returns>True only for a supported flat landing at the requested physical floor.</returns>
    public bool IsDestination(NavGraph graph, FloorLayout layout, int node, int floor)
    {
        return node >= 0 && node < graph.Nodes.Count && floor >= 0 && floor < layout.Levels.Count
            && layout.LandingNodes.Contains(node)
            && graph.Nodes[node].Flat
            && graph.Nodes[node].Floor == floor
            && Math.Abs(graph.Nodes[node].Position.y - layout.Levels[floor]) <= 0.4f;
    }

    /// <summary>
    /// Matches a remembered destination elevation after rescanning, returning -1 when that floor is missing.
    /// </summary>
    /// <param name="layout">Detected architectural elevations and supported landing nodes.</param>
    /// <param name="targetHeight">Previously requested physical elevation that must remain stable across rescans.</param>
    /// <returns>Index matching the remembered elevation within tolerance, or -1.</returns>
    public int Resolve(FloorLayout layout, float targetHeight)
    {
        if (layout.Levels.Count == 0)
        {
            return -1;
        }

        var closest = FloorDetector.Nearest(layout.Levels, targetHeight);
        return Math.Abs(layout.Levels[closest] - targetHeight) <= 0.75f ? closest : -1;
    }
}
