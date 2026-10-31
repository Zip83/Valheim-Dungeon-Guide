using System;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Handles explicit map targets without owning viewport movement or native world-map pins.</summary>
internal sealed class MapTargetSelector
{
    private readonly Action<ChestRecord> _selectChest;
    private readonly WaypointActions _waypoints;

    /// <summary>Connects existing chest navigation and personal-location workflows.</summary>
    public MapTargetSelector(Action<ChestRecord> selectChest, WaypointActions waypoints)
    {
        _selectChest = selectChest;
        _waypoints = waypoints;
    }

    /// <summary>Places with Alt, renames a nearby location with Shift, otherwise selects a visible target.</summary>
    public void Click(NativeMapLayer layer, DungeonSession session, MapViewState state,
        MapTextureRenderer renderer, Vector2 mouse, float width)
    {
        if (!CurrentSnapshot(session, renderer))
        {
            return;
        }

        var uv = layer.MouseUV(mouse);
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
        {
            Place(session, state, renderer, uv);
            return;
        }

        var waypoint = NearestWaypoint(session, renderer, uv, width);
        if (waypoint != null)
        {
            SelectWaypoint(session, waypoint);
            return;
        }

        SelectChest(session, state, renderer, uv, width);
    }

    private void Place(DungeonSession session, MapViewState state, MapTextureRenderer renderer, Vector2 uv)
    {
        var node = WaypointMapPolicy.Resolve(renderer.Published, uv.x, uv.y, session.Exploration, state.RevealAll);
        if (node != null)
        {
            _waypoints.Create(session, node.Position);
        }
    }

    private void SelectWaypoint(DungeonSession session, WaypointRecord record)
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            _waypoints.Rename(session, record);
        }
        else
        {
            _waypoints.Select(session, record);
        }
    }

    /// <summary>Removes only a nearby personal marker; empty space and chest markers are unaffected.</summary>
    public void Delete(NativeMapLayer layer, DungeonSession session, MapTextureRenderer renderer,
        Vector2 mouse, float width)
    {
        if (!CurrentSnapshot(session, renderer))
        {
            return;
        }

        var record = NearestWaypoint(session, renderer, layer.MouseUV(mouse), width);
        if (record != null)
        {
            _waypoints.Remove(session, record);
        }
    }

    private static bool CurrentSnapshot(DungeonSession session, MapTextureRenderer renderer) =>
        renderer.Published != null && renderer.Published.Graph == session.Scan?.Graph && renderer.DisplayedFloor >= 0;

    private static WaypointRecord NearestWaypoint(DungeonSession session, MapTextureRenderer renderer,
        Vector2 uv, float width)
    {
        var record = session.Data.waypoints.Where(w =>
                FloorDetector.Nearest(session.Floors, w.position.y) == renderer.DisplayedFloor && renderer.IsVisible(w.position))
            .OrderBy(w => Vector2.Distance(renderer.UV(w.position), uv)).FirstOrDefault();
        return record != null && Vector2.Distance(renderer.UV(record.position), uv) < width * 0.025f ? record : null;
    }

    private void SelectChest(DungeonSession session, MapViewState state, MapTextureRenderer renderer,
        Vector2 uv, float width)
    {
        var chest = session.Data.chests.Where(c => !c.done && (state.RevealAll || c.discovered || c.collect)
                && FloorDetector.Nearest(session.Floors, c.position.y) == renderer.DisplayedFloor
                && renderer.IsVisible(c.position))
            .OrderBy(c => Vector2.Distance(renderer.UV(c.position), uv)).FirstOrDefault();
        if (chest != null && Vector2.Distance(renderer.UV(chest.position), uv) < width * 0.025f)
        {
            _selectChest(chest);
        }
    }
}
