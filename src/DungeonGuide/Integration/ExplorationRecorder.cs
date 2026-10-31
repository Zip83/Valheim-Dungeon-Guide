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
    private IEnumerator<NavNode> _candidates;
    private float _retry;

    /// <summary>Uses the scanner's physics scheduler instead of issuing unbounded reveal queries.</summary>
    public ExplorationRecorder(GeometryPhysics physics, GuideSettings settings)
    {
        _physics = physics;
        _settings = settings;
        _visible = Visible;
    }

    /// <summary>Advances the latest player neighborhood and retries blocked samples even while stationary.</summary>
    public void Track(ScanResult scan, Exploration exploration, Vector3 player, int physicalFloor)
    {
        if (_scan != scan || _floor != physicalFloor || (player - _origin).sqrMagnitude > 0.25f
            || _candidates == null && Time.unscaledTime >= _retry)
        {
            _candidates?.Dispose();
            _scan = scan;
            _origin = player;
            _floor = physicalFloor;
            _candidates = Candidates(scan, player, Mathf.Clamp(_settings.ExploreRadius.Value, 1, 100)).GetEnumerator();
        }

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

    private void Advance(Exploration exploration, Vector3 player, int physicalFloor, FrameWorkBudget work)
    {
        while (_candidates != null && work.TryStartWork())
        {
            if (!_candidates.MoveNext())
            {
                _candidates.Dispose();
                _candidates = null;
                _retry = Time.unscaledTime + 0.25f;
                break;
            }

            var node = _candidates.Current;
            if (node != null)
            {
                ExplorationRevealPolicy.TryReveal(exploration, node, UnityPoints.P(player), physicalFloor,
                    _visible, Mathf.Clamp(_settings.ExploreRadius.Value, 1, 100));
            }
        }
    }

    /// <summary>Releases pending candidates and the previous session's graph.</summary>
    public void Reset()
    {
        _candidates?.Dispose();
        _candidates = null;
        _scan = null;
        _floor = -1;
        _retry = 0;
    }

    private bool Visible(Point a, Point b) => _physics.Visible(UnityPoints.V(a), UnityPoints.V(b));

    private static IEnumerable<NavNode> Candidates(ScanResult scan, Vector3 player, float range)
    {
        var cx = Mathf.RoundToInt(player.x / scan.Spacing);
        var cz = Mathf.RoundToInt(player.z / scan.Spacing);
        var radius = Mathf.CeilToInt(range / scan.Spacing);
        for (var ring = 0; ring <= radius; ring++)
        {
            for (var dx = -ring; dx <= ring; dx++)
            {
                foreach (var node in Column(scan, cx + dx, cz - ring))
                {
                    yield return node;
                }

                if (ring > 0)
                {
                    foreach (var node in Column(scan, cx + dx, cz + ring))
                    {
                        yield return node;
                    }
                }
            }

            for (var dz = -ring + 1; dz < ring; dz++)
            {
                foreach (var node in Column(scan, cx - ring, cz + dz))
                {
                    yield return node;
                }

                foreach (var node in Column(scan, cx + ring, cz + dz))
                {
                    yield return node;
                }
            }
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
