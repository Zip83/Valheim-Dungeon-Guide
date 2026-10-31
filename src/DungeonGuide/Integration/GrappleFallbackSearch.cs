using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Owns a cancellable, budgeted search for one hook crossing after ordinary routing has failed.</summary>
internal sealed class GrappleFallbackSearch
{
    private readonly GeometryPhysics _physics;
    private GrappleRouteProbe _probe;
    private IEnumerator<object> _job;
    private ScanResult _scan;
    private GrappleEquipment _hook;
    private int _start = -1;
    private bool _breakables;
    private float _retry;
    private Predicate<int> _target;
    private RoutePolicy _policy;

    /// <summary>Latest completed fallback route; an empty list never represents a verified crossing.</summary>
    public List<int> Route => _probe.Route;

    /// <summary>Whether geometry checks are still running across frames.</summary>
    public bool Pending => _job != null;

    /// <summary>Checked surface at which the player must aim the hook.</summary>
    public Vector3 Anchor => _probe.Anchor;

    /// <summary>Uses the existing scan/exploration budget rather than starting an independent physics allowance.</summary>
    public GrappleFallbackSearch(GeometryPhysics physics)
    {
        _physics = physics;
        _probe = new GrappleRouteProbe(physics);
    }

    /// <summary>Recognizes only the explicit forward hook section of the current result.</summary>
    public bool IsPassage(int from, int to) => _probe.IsPassage(from, to);

    /// <summary>Discards in-flight work and results on goal, session, or geometry changes.</summary>
    public void Reset()
    {
        _job?.Dispose();
        _job = null;
        _scan = null;
        _hook = null;
        _target = null;
        _policy = null;
        _probe = new GrappleRouteProbe(_physics);
    }

    /// <summary>Starts a fallback only for a usable inventory hook, retaining equivalent in-flight work.</summary>
    public void Request(ScanResult scan, int start, Predicate<int> target, RoutePolicy policy, Player player)
    {
        var hook = GrappleEquipment.Find(player);
        if (hook == null)
        {
            Reset();
            return;
        }

        if (_scan == scan && Route.Count > 0 && !_probe.OrdinarySegmentsValid(scan.Graph, policy))
        {
            Reset();
        }

        if (TryRetain(scan, start, target, policy, hook))
        {
            return;
        }

        Reset();
        _scan = scan;
        _start = start;
        _breakables = policy.Breakables;
        _hook = hook;
        _target = target;
        _policy = policy;
        _job = _probe.Find(scan, start, target, policy, hook).GetEnumerator();
    }

    private bool TryRetain(ScanResult scan, int start, Predicate<int> target, RoutePolicy policy, GrappleEquipment hook)
    {
        if (_scan == scan && _breakables == policy.Breakables && _hook?.Item == hook.Item)
        {
            if (Pending || Route.Count > 0 && _probe.Rebase(scan.Graph, start, policy)
                || Route.Count == 0 && _start == start && Time.unscaledTime < _retry)
            {
                _start = start;
                _target = target;
                _policy = policy;
                return true;
            }
        }

        return false;
    }

    /// <summary>Advances one cooperative slice and rejects results from a replaced scan or missing hook.</summary>
    public void Advance(DungeonSession session, Player player)
    {
        if (_scan == null)
        {
            return;
        }

        if (!CanContinue(session, player))
        {
            Reset();
            return;
        }

        if (_job == null)
        {
            if (Route.Count == 0 || Time.unscaledTime < _retry)
            {
                return;
            }

            _job = _probe.Recheck(_scan, _hook).GetEnumerator();
        }

        AdvanceJob();
        if (!Pending && Route.Count > 0
            && (!_target(Route[Route.Count - 1]) || !_probe.OrdinarySegmentsValid(_scan.Graph, _policy)
                || !_probe.Rebase(_scan.Graph, _start, _policy)))
        {
            Reset();
        }
    }

    private bool CanContinue(DungeonSession session, Player player)
    {
        return _scan == session?.Scan && _hook != null && _hook.IsAvailable(player);
    }

    private void AdvanceJob()
    {
        var work = _physics.Work;
        work.BeginFrame();
        work.Resume();
        try
        {
            if (!_job.MoveNext())
            {
                _job.Dispose();
                _job = null;
                _retry = Time.unscaledTime + 2;
            }
        }
        finally
        {
            work.Pause();
        }
    }
}
