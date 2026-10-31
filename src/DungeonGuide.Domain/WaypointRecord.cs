using System;

namespace DungeonGuide;

/// <summary>Stores a personal return location independently of containers and temporary floor numbers.</summary>
[Serializable]
public sealed class WaypointRecord
{
    /// <summary>Stable identity within the owning world/dungeon save.</summary>
    public string id;

    /// <summary>Player-owned name preserved across game-language changes.</summary>
    public string label;

    /// <summary>Physical world position, including elevation for stacked floor resolution.</summary>
    public Point position;
}
