using UnityEngine;

namespace DungeonGuide;

/// <summary>Coordinates explicit personal-location commands, persistence and navigation selection.</summary>
internal sealed class WaypointActions
{
    private readonly GuideRuntime _runtime;
    private readonly SessionLifecycle _lifecycle;
    private readonly NavigationCoordinator _navigator;
    private readonly NoteDialog _dialog;
    private readonly UserNotice _notice;
    private readonly GuideText _text;

    /// <summary>Connects existing session, save, route and native-note services without owning their lifetimes.</summary>
    public WaypointActions(GuideRuntime runtime, SessionLifecycle lifecycle, NavigationCoordinator navigator,
        NoteDialog dialog, UserNotice notice, GuideText text)
    {
        _runtime = runtime;
        _lifecycle = lifecycle;
        _navigator = navigator;
        _dialog = dialog;
        _notice = notice;
        _text = text;
    }

    /// <summary>Dispatches location commands; false leaves unrelated commands to their existing owners.</summary>
    public bool TryExecute(GuideCommand command, DungeonSession session)
    {
        switch (command)
        {
            case GuideCommand.MarkLocation:
                if (Player.m_localPlayer)
                {
                    Create(session, UnityPoints.P(Player.m_localPlayer.transform.position));
                }
                return true;
            case GuideCommand.NextLocation:
                Select(session, new WaypointRegistry(session.Data.waypoints).Next(_navigator.Waypoint));
                return true;
            case GuideCommand.RenameLocation:
                Rename(session, _navigator.Waypoint);
                return true;
            case GuideCommand.RemoveLocation:
                Remove(session, _navigator.Waypoint);
                return true;
            default:
                return false;
        }
    }

    /// <summary>Stores an exact location even before scan/navigation readiness, then requests its return route.</summary>
    public void Create(DungeonSession session, Point position)
    {
        if (!Writable(session))
        {
            return;
        }

        var record = new WaypointRegistry(session.Data.waypoints).Add(position,
            _text.WaypointPrefix + (session.Data.waypoints.Count + 1));
        _navigator.ToWaypoint(record);
        SaveNotice(session, _text.WaypointAdded + record.label);
    }

    /// <summary>Selects only a marker belonging to the active session, without changing exploration.</summary>
    public void Select(DungeonSession session, WaypointRecord record)
    {
        if (record == null || !session.Data.waypoints.Contains(record))
        {
            _notice.Show(_text.NoWaypoint);
            return;
        }

        _navigator.ToWaypoint(record);
        _notice.Show(_text.WaypointSelected + record.label);
    }

    /// <summary>Opens native note editing and rejects stale callbacks after deletion, session exit or replacement.</summary>
    public void Rename(DungeonSession session, WaypointRecord record)
    {
        if (!Owned(session, record) || !Writable(session))
        {
            return;
        }

        _dialog.Request(record.label, _text.WaypointNamePrompt, value => AcceptName(session, record, value));
    }

    private void AcceptName(DungeonSession session, WaypointRecord record, string value)
    {
        if (_runtime.Session != session || session.ReadOnly)
        {
            return;
        }

        if (new WaypointRegistry(session.Data.waypoints).Rename(record, value))
        {
            SaveNotice(session, _text.ChestSaved + record.label);
        }
    }

    /// <summary>Deletes only the explicit marker and stops navigation only when it targeted that marker.</summary>
    public void Remove(DungeonSession session, WaypointRecord record)
    {
        if (!Owned(session, record) || !Writable(session))
        {
            return;
        }

        new WaypointRegistry(session.Data.waypoints).Remove(record);
        if (_navigator.Waypoint == record)
        {
            _navigator.Stop();
        }

        SaveNotice(session, _text.WaypointRemoved + record.label);
    }

    private bool Owned(DungeonSession session, WaypointRecord record)
    {
        if (record != null && session.Data.waypoints.Contains(record))
        {
            return true;
        }

        _notice.Show(_text.NoWaypoint);
        return false;
    }

    private bool Writable(DungeonSession session)
    {
        if (_runtime.Session == session && !session.ReadOnly)
        {
            return true;
        }

        _notice.Show(_text.SaveWriteFailed);
        return false;
    }

    private void SaveNotice(DungeonSession session, string message)
    {
        if (_lifecycle.Save(session))
        {
            _notice.Show(message);
        }
    }
}
