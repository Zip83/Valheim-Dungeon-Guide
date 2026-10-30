using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Holds detected floor elevations and the nodes that can safely qualify as floor destinations.
/// </summary>
public sealed class FloorLayout
{
    /// <summary>
    /// Ascending world-space elevations of the detected architectural floors.
    /// </summary>
    public List<float> Levels { get; } = new List<float>();

    /// <summary>
    /// Graph node indices in broad floor interiors, excluding narrow balconies and bridges.
    /// </summary>
    public HashSet<int> LandingNodes { get; } = new HashSet<int>();
}
