using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Rechecks restored door and destructible permissions against the current loaded scene under the shared budget.</summary>
internal sealed class RestoredPassageValidation
{
    private readonly GeometryPhysics _physics;
    private readonly DoorPassageVerifier _doors;
    private readonly BreakablePassageVerifier _breakables;
    private readonly List<Collider> _blockers = new();
    private DungeonSession _session;
    private ScanResult _scan;
    private IEnumerator<object> _work;

    /// <summary>Uses the same native recognition and physical allowance as ordinary scanning.</summary>
    public RestoredPassageValidation(GeometryPhysics physics, DoorPassageVerifier doors, BreakablePassageVerifier breakables)
    {
        _physics = physics;
        _doors = doors;
        _breakables = breakables;
    }

    /// <summary>Keeps the cached map visible while fresh special-passage checks prepare its navigation.</summary>
    public void Begin(DungeonSession session)
    {
        Cancel();
        if (session.HasPartialGeometry || session.GeometryNeedsScan)
        {
            session.NavigationReady = false;
            return;
        }

        _session = session;
        _scan = session.Scan;
        session.NavigationReady = false;
        _work = Verify().GetEnumerator();
    }

    /// <summary>Advances checks only for the matching snapshot after the player's native transfer finishes.</summary>
    public void Tick(DungeonSession current)
    {
        if (_work == null)
        {
            return;
        }

        if (current != _session || current.Scan != _scan || current.Scanning)
        {
            Cancel();
            return;
        }

        if (Player.m_localPlayer && Player.m_localPlayer.IsTeleporting())
        {
            return;
        }

        Advance();
    }

    private void Advance()
    {
        var budget = _physics.Work;
        budget.BeginFrame();
        budget.Resume();
        try
        {
            if (!_work.MoveNext())
            {
                _session.NavigationReady = true;
                Cancel();
            }
        }
        finally
        {
            budget.Pause();
        }
    }

    private IEnumerable<object> Verify()
    {
        foreach (var step in _scan.SpecialPassages.Build(_scan.Graph))
        {
            foreach (var pause in Reserve())
            {
                yield return pause;
            }
        }

        foreach (var passage in _scan.SpecialPassages.Passages)
        {
            passage.Edge.Verified = false;
            foreach (var pause in Check(_scan.Graph.Nodes[passage.Source].Position,
                _scan.Graph.Nodes[passage.Edge.To].Position, passage.Edge))
            {
                yield return pause;
            }
        }
    }

    private IEnumerable<object> Check(Point from, Point to, NavEdge edge)
    {
        var start = UnityPoints.V(from);
        var end = UnityPoints.V(to);
        foreach (var pause in Reserve())
        {
            yield return pause;
        }

        if (!_physics.Supported((start + end) * 0.5f))
        {
            yield break;
        }

        var complete = false;
        foreach (var pause in ReadBlockers(start, end, result => complete = result))
        {
            yield return pause;
        }

        if (complete)
        {
            edge.Verified = Permitted(edge.Kind);
        }
    }

    private IEnumerable<object> ReadBlockers(Vector3 start, Vector3 end, System.Action<bool> complete)
    {
        _blockers.Clear();
        foreach (var pause in Reserve())
        {
            yield return pause;
        }

        if (!_physics.AddBlockers(start + Vector3.up * 0.6f, end + Vector3.up * 0.6f, 0.18f, _blockers))
        {
            complete(false);
            yield break;
        }

        foreach (var pause in Reserve())
        {
            yield return pause;
        }

        complete(_physics.AddBlockers(start + Vector3.up * 1.25f, end + Vector3.up * 1.25f, 0.18f, _blockers));
    }

    private bool Permitted(PassageKind kind)
    {
        if (_blockers.Count == 0)
        {
            return true;
        }

        if (kind == PassageKind.Door)
        {
            return _doors.Matches(_blockers, out var door) && _doors.CanOpen(door);
        }

        return _breakables.Matches(_blockers, out _, out var verified) && verified;
    }

    private IEnumerable<object> Reserve()
    {
        while (!_physics.Work.TryStartWork())
        {
            yield return null;
            _physics.Work.BeginFrame();
        }
    }

    /// <summary>Discards pending checks when the session or its geometry changes.</summary>
    public void Cancel()
    {
        _work?.Dispose();
        _work = null;
        _session = null;
        _scan = null;
        _blockers.Clear();
    }
}
