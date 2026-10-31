using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Retains one connected stair region on one floor independently of current exploration.</summary>
internal sealed class StairMarkerGroup
{
    /// <summary>Actual graph samples eligible for visible upward or downward symbols.</summary>
    public List<int> Nodes { get; } = new();

    /// <summary>Horizontal center used to choose a central sample across a wide landing.</summary>
    public Point Center;
}
