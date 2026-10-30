using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Coordinates cooperative floor sampling and passage linking under one per-frame budget.
/// </summary>
internal sealed class GeometryScanner
{
    private readonly GeometryPhysics _physics;
    private readonly ScanColumnSampler _sampler;
    private readonly ScanPassageLinker _linker;
    private FrameWorkBudget _work;

    /// <summary>Current geometry phase, independent of subsequent floor analysis.</summary>
    public GeometryScanPhase Phase
    {
        get; private set;
    }

    /// <summary>Number of fully sampled columns in the current scan.</summary>
    public int ProcessedColumns
    {
        get; private set;
    }

    /// <summary>Number of floor nodes recorded through the most recently completed column.</summary>
    public int SampledNodes
    {
        get; private set;
    }

    /// <summary>Cumulative operations admitted by the current scan's shared work budget.</summary>
    public long WorkSteps => _work?.TotalSteps ?? 0;

    /// <summary>
    /// Composes the sampling and linking phases using shared main-thread physics services.
    /// </summary>
    /// <param name="physics">Shared main-thread collision queries and overflow counter.</param>
    /// <param name="passages">Evaluator for supported ordinary and obstructed passages.</param>
    /// <param name="jumps">Evaluator for unverified airborne gap candidates.</param>
    public GeometryScanner(GeometryPhysics physics, PassageEvaluator passages, JumpEvaluator jumps)
    {
        _physics = physics;
        _sampler = new ScanColumnSampler(physics);
        _linker = new ScanPassageLinker(passages, new ScanGapLinker(physics, passages, jumps));
    }

    /// <summary>
    /// Defers physics until a later frame, then publishes the completed graph and completeness metadata.
    /// </summary>
    /// <param name="area">Loaded or explicitly requested region to sample.</param>
    /// <param name="spacing">Horizontal distance between sampling columns in metres.</param>
    /// <param name="limit">Maximum number of columns to sample before marking the result truncated.</param>
    /// <param name="stepsPerFrame">Maximum indivisible work steps between frame yields.</param>
    /// <param name="millisecondsPerFrame">Soft elapsed-time budget per frame in milliseconds.</param>
    /// <param name="complete">Callback receiving the graph after all sampled columns have been linked.</param>
    /// <returns>Coroutine yielding frames before physics work and whenever the shared work budget is
    /// exhausted.</returns>
    public IEnumerator Scan(
        DungeonArea area, float spacing, int limit, int stepsPerFrame,
        float millisecondsPerFrame, Action<ScanResult> complete)
    {
        // StartCoroutine runs until the first yield; do no physics work in the input handler.
        ResetProgress();
        yield return null;
        var work = new FrameWorkBudget(stepsPerFrame, millisecondsPerFrame);
        _work = work;
        var overflowRevision = _physics.BufferOverflows;
        var result = new ScanResult { Spacing = spacing };

        foreach (var pause in SampleColumns(area, result, limit, work))
        {
            yield return pause;
        }

        Phase = GeometryScanPhase.Linking;
        foreach (var pause in _linker.Link(result, work))
        {
            yield return pause;
        }

        result.Truncated |= _physics.BufferOverflows != overflowRevision;
        Phase = GeometryScanPhase.Complete;
        complete(result);
    }

    private void ResetProgress()
    {
        Phase = GeometryScanPhase.Sampling;
        ProcessedColumns = 0;
        SampledNodes = 0;
        _work = null;
    }

    private IEnumerable<object> SampleColumns(
        DungeonArea area, ScanResult result, int limit, FrameWorkBudget work)
    {
        var count = 0;
        foreach (var column in ColumnsWithin(area.Bounds, result.Spacing))
        {
            if (count++ >= limit || result.Graph.Nodes.Count >= 100000)
            {
                result.Truncated = true;
                yield break;
            }

            foreach (var pause in _sampler.Sample(area, result, column.x, column.z, work))
            {
                yield return pause;
            }

            ProcessedColumns++;
            SampledNodes = result.Graph.Nodes.Count;
        }
    }

    private static IEnumerable<(int x, int z)> ColumnsWithin(Bounds bounds, float spacing)
    {
        var x0 = Mathf.FloorToInt(bounds.min.x / spacing);
        var x1 = Mathf.CeilToInt(bounds.max.x / spacing);
        var z0 = Mathf.FloorToInt(bounds.min.z / spacing);
        var z1 = Mathf.CeilToInt(bounds.max.z / spacing);
        for (var x = x0; x <= x1; x++)
        {
            for (var z = z0; z <= z1; z++)
            {
                yield return (x, z);
            }
        }
    }
}
