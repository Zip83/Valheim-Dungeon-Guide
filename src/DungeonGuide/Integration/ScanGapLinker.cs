using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Evaluates small cardinal gaps and records only unverified, directional jump or drop candidates.
/// </summary>
internal sealed class ScanGapLinker
{
    private readonly GeometryPhysics _physics;
    private readonly PassageEvaluator _passages;
    private readonly JumpEvaluator _jumps;

    /// <summary>
    /// Keeps supported-door checks ahead of airborne candidate evaluation.
    /// </summary>
    /// <param name="physics">Shared main-thread support and clearance queries.</param>
    /// <param name="passages">Supported-passage evaluator used before considering an airborne gap.</param>
    /// <param name="jumps">Cooperative jump-arc evaluator.</param>
    public ScanGapLinker(GeometryPhysics physics, PassageEvaluator passages, JumpEvaluator jumps)
    {
        _physics = physics;
        _passages = passages;
        _jumps = jumps;
    }

    /// <summary>
    /// Evaluates stacked-height pairs lazily using the scan's shared per-frame budget.
    /// </summary>
    /// <param name="result">Private sampled graph receiving unverified directed candidates.</param>
    /// <param name="column">Existing source column in the horizontal grid index.</param>
    /// <param name="work">Budget shared with all other scan phases.</param>
    /// <returns>Frame pauses while candidate pairs and their required physics queries are evaluated.</returns>
    public IEnumerable<object> LinkColumn(ScanResult result, (int x, int z) column, FrameWorkBudget work)
    {
        foreach (var pair in result.Pairs.Gaps[column])
        {
            while (!work.TryStartWork())
            {
                yield return null;
                work.BeginFrame();
            }

            if (HasConnection(result.Graph, pair.from, pair.to))
            {
                continue;
            }

            // A closed door or grate can occupy the missing intermediate cell.
            _passages.Connect(result.Graph, pair.from, pair.to);
            if (!TryGetGapEndpoints(result, pair.from, pair.to, out var start, out var end))
            {
                continue;
            }

            foreach (var pause in EvaluateJump(result, pair.from, pair.to, start, end, work))
            {
                yield return pause;
            }
        }
    }

    private bool TryGetGapEndpoints(ScanResult result, int from, int to, out Vector3 start, out Vector3 end)
    {
        start = UnityPoints.V(result.Graph.Nodes[from].Position);
        end = UnityPoints.V(result.Graph.Nodes[to].Position);
        return !HasConnection(result.Graph, from, to) && !_physics.Supported((start + end) * 0.5f);
    }

    private IEnumerable<object> EvaluateJump(
        ScanResult result, int from, int to, Vector3 start, Vector3 end, FrameWorkBudget work)
    {
        var candidate = false;
        foreach (var pause in _jumps.Candidate(start, end, work, valid => candidate = valid))
        {
            yield return pause;
        }

        if (candidate)
        {
            RecordCandidate(result, from, to, end.y < start.y - 0.6f ? PassageKind.Drop : PassageKind.Jump);
        }
    }

    private static bool HasConnection(NavGraph graph, int from, int to)
    {
        return graph.Nodes[from].Edges.Exists(edge => edge.To == to);
    }

    private static void RecordCandidate(ScanResult result, int from, int to, PassageKind kind)
    {
        result.Graph.Connect(from, to, kind, false, false);
        var source = result.Graph.Nodes[from];
        var destination = result.Graph.Nodes[to];
        result.TraversalCandidates[(source.Key, destination.Key)] = source.Edges[source.Edges.Count - 1];
    }
}
