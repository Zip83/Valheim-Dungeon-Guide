using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Adds supported ordinary, stair, door, and approved-breakable graph connections.
/// </summary>
internal sealed class PassageEvaluator
{
    private readonly List<Collider> _blockers = new(32);
    private readonly GeometryPhysics _physics;
    private readonly DoorPassageVerifier _doors;
    private readonly BreakablePassageVerifier _breakables;

    /// <summary>
    /// Connects collision, door, and approved-breakable checks without approving unknown traversal.
    /// </summary>
    public PassageEvaluator(GeometryPhysics physics, DoorPassageVerifier doors, BreakablePassageVerifier breakables)
    {
        _physics = physics;
        _doors = doors;
        _breakables = breakables;
    }

    /// <summary>
    /// Qualifies endpoints, gathers blockers, then delegates passage-specific connection rules.
    /// </summary>
    public void Connect(NavGraph graph, int from, int to)
    {
        var start = UnityPoints.V(graph.Nodes[from].Position);
        var end = UnityPoints.V(graph.Nodes[to].Position);
        var horizontal = new Vector2(start.x - end.x, start.z - end.z).magnitude;
        if (!WithinSlopeLimit(start, end, horizontal) || !HasIntermediateSupport(start, end, horizontal))
        {
            return;
        }

        if (ReadBlockers(start, end))
        {
            ConnectPassage(graph, from, to, start, end);
        }
    }

    private static bool WithinSlopeLimit(Vector3 start, Vector3 end, float horizontal)
    {
        return Mathf.Abs(start.y - end.y) <= horizontal * 0.85f + 0.18f;
    }

    private bool HasIntermediateSupport(Vector3 start, Vector3 end, float horizontal)
    {
        return horizontal <= 1.9f || _physics.Supported(Vector3.Lerp(start, end, 0.25f))
            && _physics.Supported(Vector3.Lerp(start, end, 0.75f));
    }

    private bool ReadBlockers(Vector3 start, Vector3 end)
    {
        _blockers.Clear();
        return _physics.AddBlockers(start + Vector3.up * 0.6f, end + Vector3.up * 0.6f, 0.18f, _blockers)
            && _physics.AddBlockers(start + Vector3.up * 1.25f, end + Vector3.up * 1.25f, 0.18f, _blockers);
    }

    private void ConnectPassage(NavGraph graph, int from, int to, Vector3 start, Vector3 end)
    {
        if (_blockers.Count == 0)
        {
            ConnectWalk(graph, from, to, start, end);
        }
        else if (_doors.Matches(_blockers, out var door))
        {
            ConnectDoor(graph, from, to, start, end, door);
        }
        else
        {
            ConnectBreakable(graph, from, to, start, end);
        }
    }

    private void ConnectWalk(NavGraph graph, int from, int to, Vector3 start, Vector3 end)
    {
        if (_physics.Supported((start + end) * 0.5f))
        {
            var kind = Mathf.Abs(start.y - end.y) > 0.25f ? PassageKind.Stairs : PassageKind.Walk;
            graph.Connect(from, to, kind);
        }
    }

    private void ConnectDoor(NavGraph graph, int from, int to, Vector3 start, Vector3 end, Door door)
    {
        // Locked doors require the player's actual key; planning never opens anything.
        var canOpen = _doors.CanOpen(door);
        if (_physics.Supported((start + end) * 0.5f))
        {
            graph.Connect(from, to, PassageKind.Door, canOpen, true, door.name);
        }
    }

    private void ConnectBreakable(NavGraph graph, int from, int to, Vector3 start, Vector3 end)
    {
        if (_breakables.Matches(_blockers, out var prefab, out var verified)
            && _physics.Supported((start + end) * 0.5f))
        {
            graph.Connect(from, to, PassageKind.Breakable, verified, true, prefab);
        }
    }
}
