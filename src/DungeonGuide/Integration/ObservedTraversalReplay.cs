using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Rebinds saved or newly observed traversals to current floor samples under the shared physics budget.</summary>
internal sealed class ObservedTraversalReplay
{
    private readonly GeometryPhysics _physics;
    private readonly JumpEvaluator _jumps;
    private ScanResult _scan;
    private int _count = -1;
    private IEnumerator<object> _work;

    /// <summary>Uses cooperative landing and arc checks before creating any missing directed edge.</summary>
    public ObservedTraversalReplay(GeometryPhysics physics)
    {
        _physics = physics;
        _jumps = new JumpEvaluator(physics);
    }

    /// <summary>Whether every current proof has finished its geometry check.</summary>
    public bool Ready => _scan != null && _work == null;

    /// <summary>Schedules changed evidence and advances only within the scanner's existing frame allowance.</summary>
    public void Tick(ScanResult scan, List<TraversalProof> proofs)
    {
        if (_scan != scan || _count != proofs.Count)
        {
            Reset();
            _scan = scan;
            _count = proofs.Count;
            _work = Replay(scan, proofs).GetEnumerator();
        }

        Advance();
    }

    private void Advance()
    {
        if (_work == null)
        {
            return;
        }

        var budget = _physics.Work;
        budget.BeginFrame();
        budget.Resume();
        try
        {
            if (!_work.MoveNext())
            {
                _work.Dispose();
                _work = null;
            }
        }
        finally
        {
            budget.Pause();
        }
    }

    private IEnumerable<object> Replay(ScanResult scan, List<TraversalProof> proofs)
    {
        for (var i = 0; i < proofs.Count; i++)
        {
            while (!_physics.Work.TryStartWork())
            {
                yield return null;
                _physics.Work.BeginFrame();
            }

            foreach (var pause in Rebind(scan, proofs[i]))
            {
                yield return pause;
            }
        }

        RemoveDuplicates(proofs);
        _count = proofs.Count;
    }

    private IEnumerable<object> Rebind(ScanResult scan, TraversalProof proof)
    {
        var from = TraversalEndpointLocator.Find(scan, proof.from);
        var to = TraversalEndpointLocator.Find(scan, proof.to);
        if (from < 0 || to < 0 || from == to)
        {
            yield break;
        }

        var clear = false;
        foreach (var pause in _jumps.Observed(UnityPoints.V(scan.Graph.Nodes[from].Position),
            UnityPoints.V(scan.Graph.Nodes[to].Position), _physics.Work, valid => clear = valid))
        {
            yield return pause;
        }

        PublishResult(scan, proof, from, to, clear);
    }

    private static void PublishResult(ScanResult scan, TraversalProof proof, int from, int to, bool clear)
    {
        if (clear)
        {
            ObservedTraversalBinding.Bind(scan.Graph, scan.TraversalCandidates, proof, from, to);
        }
        else
        {
            var key = (scan.Graph.Nodes[from].Key, scan.Graph.Nodes[to].Key);
            if (scan.TraversalCandidates.TryGetValue(key, out var edge))
            {
                edge.Verified = false;
            }

            scan.TraversalCandidates.Remove(key);
        }
    }

    private static void RemoveDuplicates(List<TraversalProof> proofs)
    {
        var known = new HashSet<(string, string)>();
        for (var i = proofs.Count - 1; i >= 0; i--)
        {
            if (!known.Add((Point.Key(proofs[i].from), Point.Key(proofs[i].to))))
            {
                proofs.RemoveAt(i);
            }
        }
    }

    /// <summary>Cancels old geometry bindings; successful evidence remains in the dungeon save.</summary>
    public void Reset()
    {
        _work?.Dispose();
        _work = null;
        _scan = null;
        _count = -1;
    }
}
