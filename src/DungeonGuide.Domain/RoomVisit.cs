using System;

namespace DungeonGuide;

/// <summary>Records a room-local entry elevation independently of temporary floor numbering.</summary>
[Serializable]
public sealed class RoomVisit
{
    /// <summary>Stable generated-room identity within the saved dungeon.</summary>
    public string room;

    /// <summary>Entry position in the room's local coordinates, retained across rescans.</summary>
    public Point local;
}
