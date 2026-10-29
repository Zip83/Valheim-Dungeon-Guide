using System;
using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Adds supported ordinary, stair, door, and approved-breakable graph connections.
/// </summary>
internal sealed class PassageEvaluator
{
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
    /// Adds verified ordinary or supported special connections between two sampled floor nodes.
    /// </summary>
    public void Connect(NavGraph graph, int a, int b)
    {
        var from = UnityPoints.V(graph.Nodes[a].Position);
        var to = UnityPoints.V(graph.Nodes[b].Position);
        var horizontal = new Vector2(from.x - to.x, from.z - to.z).magnitude;
        if (Mathf.Abs(from.y - to.y) > horizontal * 0.85f + 0.18f)
        {
            return;
        }

        if (horizontal > 1.9f
            && (!_physics.Supported(Vector3.Lerp(from, to, 0.25f))
            || !_physics.Supported(Vector3.Lerp(from, to, 0.75f))))
        {
            return;
        }

        var blockers = _physics.Blockers(from + Vector3.up * 0.6f, to + Vector3.up * 0.6f, 0.18f)
            .Concat(_physics.Blockers(from + Vector3.up * 1.25f, to + Vector3.up * 1.25f, 0.18f))
            .Distinct()
            .ToArray();
        if (blockers.Length == 0 && _physics.Supported((from + to) * 0.5f))
        {
            graph.Connect(a, b, Mathf.Abs(from.y - to.y) > 0.25f ? PassageKind.Stairs : PassageKind.Walk);
            return;
        }

        if (blockers.Length == 0)
        {
            return;
        }

        if (_doors.Matches(blockers, out var door))
        {
            // Locked doors require the player's actual key; planning never opens anything.
            var key = _doors.CanOpen(door);
            if (_physics.Supported((from + to) * 0.5f))
            {
                graph.Connect(a, b, PassageKind.Door, key, true, door.name);
            }

            return;
        }

        if (_breakables.Matches(blockers, out var prefab, out var verified)
            && _physics.Supported((from + to) * 0.5f))
        {
            graph.Connect(a, b, PassageKind.Breakable, verified, true, prefab);
        }
    }
}
