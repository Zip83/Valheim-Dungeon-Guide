using UnityEngine;

namespace DungeonGuide;

/// <summary>Records actual entries in oriented room volumes without requiring scan geometry.</summary>
internal static class RoomEntryRecorder
{
    /// <summary>Persists local entry anchors without inferring visits from remotely revealed samples.</summary>
    public static void Track(DungeonSession session, Vector3 player)
    {
        foreach (var room in session.Area.RoomVolumes)
        {
            if (!room.Contains(player))
            {
                continue;
            }

            var local = room.Local(player);
            if (!session.Data.visitedRooms.Exists(visit => visit != null && visit.room == room.Id
                && Mathf.Abs(visit.local.y - local.y) < 1.2f))
            {
                session.Data.visitedRooms.Add(new RoomVisit { room = room.Id, local = local });
            }
        }
    }
}
