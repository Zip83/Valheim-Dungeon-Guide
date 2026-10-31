using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Visits indexed columns near-first and budgets every visibility query on the game thread.</summary>
internal sealed class ExplorationRecorder
{
    private readonly GeometryPhysics _physics;
    private readonly GuideSettings _settings;
    private readonly Func<Point, Point, bool> _visible;
    private ScanResult _scan;
    private Vector3 _origin;
    private int _floor = -1;
    private readonly ExplorationSweep _sweep = new();
    private Vector3 _eyes;
    private float _retry;

    /// <summary>Uses the scanner's physics scheduler instead of issuing unbounded reveal queries.</summary>
    public ExplorationRecorder(GeometryPhysics physics, GuideSettings settings)
    {
        _physics = physics;
        _settings = settings;
        _visible = Visible;
    }

    /// <summary>Advances the latest player neighborhood and retries blocked samples even while stationary.</summary>
    public void Track(ScanResult scan, Exploration exploration, Vector3 player, Vector3 eyes, int physicalFloor)
    {
        _eyes = eyes;
        ObservePosition(scan, player, physicalFloor);
        RefreshDistantSweep(scan, player);

        var work = _physics.Work;
        work.BeginFrame();
        work.Resume();
        try
        {
            Advance(exploration, player, physicalFloor, work);
        }
        finally
        {
            work.Pause();
        }
    }

    private void ObservePosition(ScanResult scan, Vector3 player, int physicalFloor)
    {
        var changed = _scan != scan || _floor != physicalFloor;
        if (changed)
        {
            _sweep.Reset();
            _scan = scan;
            _floor = physicalFloor;
            _retry = 0;
        }

        if (changed || (player - _origin).sqrMagnitude > 0.25f)
        {
            _origin = player;
            _sweep.RefreshNearby(Candidates(scan, player, Mathf.Min(3, Radius)));
        }
    }

    private float Radius => Mathf.Clamp(_settings.ExploreRadius.Value, 1, ExplorationRevealPolicy.MaximumRadius);

    private void RefreshDistantSweep(ScanResult scan, Vector3 player)
    {
        if (!_sweep.HasDistant && Time.unscaledTime >= _retry)
        {
            _sweep.RefreshDistant(Candidates(scan, player, Radius));
            _retry = Time.unscaledTime + 0.25f;
        }
    }

    private void Advance(Exploration exploration, Vector3 player, int physicalFloor, FrameWorkBudget work)
    {
        while (work.TryStartWork())
        {
            if (!TryTakeCandidate(out var node))
            {
                break;
            }

            if (node != null)
            {
                ExplorationRevealPolicy.TryReveal(exploration, node, UnityPoints.P(player), physicalFloor,
                    _visible, Radius);
            }
        }
    }

    private bool TryTakeCandidate(out NavNode node)
    {
        var hadDistant = _sweep.HasDistant;
        var available = _sweep.TryTake(out node);
        if (hadDistant && !_sweep.HasDistant)
        {
            _retry = Time.unscaledTime + 0.25f;
        }

        return available;
    }

    /// <summary>Releases pending candidates and the previous session's graph.</summary>
    public void Reset()
    {
        _sweep.Reset();
        _scan = null;
        _floor = -1;
        _retry = 0;
    }

    private bool Visible(Point a, Point b) =>
        _physics.Clear(_eyes, UnityPoints.V(b) + Vector3.up * 0.65f, 0.1f) || NearbyFloorVisible(a, b);

    private bool NearbyFloorVisible(Point player, Point sample) =>
        Point.Distance(player, sample) <= 3 && _physics.Work.TryStartWork()
        && _physics.Visible(UnityPoints.V(player), UnityPoints.V(sample));

    private static IEnumerable<NavNode> Candidates(ScanResult scan, Vector3 player, float range)
    {
        var cx = Mathf.RoundToInt(player.x / scan.Spacing);
        var cz = Mathf.RoundToInt(player.z / scan.Spacing);
        var radius = Mathf.CeilToInt(range / scan.Spacing);
        for (var ring = 0; ring <= radius; ring++)
        {
            foreach (var column in Ring(cx, cz, ring))
            {
                foreach (var node in Column(scan, column.x, column.z))
                {
                    yield return node;
                }
            }
        }
    }

    private static IEnumerable<(int x, int z)> Ring(int cx, int cz, int radius)
    {
        for (var dx = -radius; dx <= radius; dx++)
        {
            yield return (cx + dx, cz - radius);
            if (radius > 0)
            {
                yield return (cx + dx, cz + radius);
            }
        }

        for (var dz = -radius + 1; dz < radius; dz++)
        {
            yield return (cx - radius, cz + dz);
            yield return (cx + radius, cz + dz);
        }
    }

    private static IEnumerable<NavNode> Column(ScanResult scan, int x, int z)
    {
        // Empty columns still consume a scheduler step; sparse scans cannot monopolize a frame.
        yield return null;
        if (scan.Columns.TryGetValue((x, z), out var column))
        {
            foreach (var id in column)
            {
                yield return scan.Graph.Nodes[id];
            }
        }
    }
}
