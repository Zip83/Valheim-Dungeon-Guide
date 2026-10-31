using System;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Constructs scan descriptors from loaded rooms or an explicitly supplied manual volume.
/// </summary>
internal sealed class DungeonAreaFactory
{
    private readonly GuideText _text;
    private readonly DungeonEntranceRecorder _entrances;

    /// <summary>
    /// Uses localized copy only for automatically supplied manual-area labels.
    /// </summary>
    /// <param name="text">Selected-language catalog for generated manual-area labels.</param>
    /// <param name="entrances">Confirmed local dungeon transfers, independent of navigation readiness.</param>
    public DungeonAreaFactory(GuideText text, DungeonEntranceRecorder entrances)
    {
        _text = text;
        _entrances = entrances;
    }

    /// <summary>
    /// Describes a generator only when one of its loaded rooms contains the position.
    /// </summary>
    /// <param name="generator">Loaded generator whose child rooms define the dungeon.</param>
    /// <param name="position">World-space position that must belong to one of the rooms.</param>
    /// <returns>Room-constrained scan descriptor, or null when the position is outside every room.</returns>
    public DungeonArea Generated(DungeonGenerator generator, Vector3 position)
    {
        var rooms = generator.GetComponentsInChildren<Room>(true);
        if (!rooms.Any(room => DungeonRoomGeometry.Contains(room, position)))
        {
            return null;
        }

        var boxes = rooms.Select(DungeonRoomGeometry.WorldBounds).ToArray();
        return new DungeonArea
        {
            Id = "dungeon_" + UnityPoints.PositionId(generator.transform.position) + "_" + generator.m_themes,
            Label = generator.m_themes.ToString(),
            Bounds = DungeonRoomGeometry.Enclose(boxes),
            Rooms = boxes,
            RoomVolumes = rooms.Select(room => new DungeonRoom(room)).ToArray(),
            Entrance = _entrances.Find(rooms) ?? FindEntrance(rooms)
        };
    }

    /// <summary>
    /// Creates a bounded manual region with its existing persistent identity and no inferred entrance.
    /// </summary>
    /// <param name="position">World-space centre used for the volume and persistent area identity.</param>
    /// <param name="radius">Horizontal half-extent in metres.</param>
    /// <param name="height">Vertical half-extent in metres.</param>
    /// <returns>Explicit manual area, with empty room constraints and no inferred entrance.</returns>
    public DungeonArea Manual(Vector3 position, float radius, float height) => new()
    {
        Id = "fortress_" + Mathf.FloorToInt(position.x / 128) + "_" + Mathf.FloorToInt(position.y / 1000) + "_"
            + Mathf.FloorToInt(position.z / 128),
        Label = _text.ManualArea,
        IsManual = true,
        Bounds = new Bounds(position, new Vector3(radius * 2, height * 2, radius * 2)),
        Rooms = Array.Empty<Bounds>()
    };

    private static Point? FindEntrance(Room[] rooms)
    {
        var entry = rooms.FirstOrDefault(room => room.m_entrance);
        if (!entry)
        {
            return null;
        }

        var connection = entry.GetConnections()?.FirstOrDefault(candidate => candidate && candidate.m_entrance);
        var position = connection ? connection.transform.position : entry.transform.position;

        return UnityPoints.P(position);
    }
}
