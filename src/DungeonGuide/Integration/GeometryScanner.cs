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
    private ScanPairAnalysisJob _pairAnalysis;

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
    /// <param name="sampleOnly">Publish display samples without preparing navigation passages.</param>
    /// <returns>Coroutine yielding frames before physics work and whenever the shared work budget is
    /// exhausted.</returns>
    public IEnumerator Scan(
        DungeonArea area, float spacing, int limit, int stepsPerFrame,
        float millisecondsPerFrame, Action<ScanResult> complete, bool sampleOnly = false)
    {
        // StartCoroutine runs until the first yield; do no physics work in the input handler.
        ResetProgress();
        yield return null;
        _physics.ConfigureBudget(stepsPerFrame, millisecondsPerFrame);
        var work = _physics.Work;
        _work = work;
        var overflowRevision = _physics.BufferOverflows;
        var result = new ScanResult { Spacing = spacing };

        foreach (var pause in SampleColumns(area, result, limit, work))
        {
            yield return pause;
        }

        if (sampleOnly)
        {
            result.Truncated |= _physics.BufferOverflows != overflowRevision;
            Phase = GeometryScanPhase.Complete;
            complete(result);
            yield break;
        }

        Phase = GeometryScanPhase.PairSelection;
        using (var analysis = new ScanPairAnalysisJob(result.Graph, result.Columns))
        {
            _pairAnalysis = analysis;
            try
            {
                PassagePairCatalog pairs;
                while (!analysis.TryComplete(out pairs))
                {
                    yield return null;
                }

                result.Pairs = pairs;
            }
            finally
            {
                _pairAnalysis = null;
            }
        }

        Phase = GeometryScanPhase.Linking;
        foreach (var pause in _linker.Link(result, work))
        {
            yield return pause;
        }

        result.Pairs = null;
        result.Truncated |= _physics.BufferOverflows != overflowRevision;
        Phase = GeometryScanPhase.Complete;
        complete(result);
    }

    /// <summary>Resumes accounting only while the scan coroutine is actually executing.</summary>
    public void BeginSlice()
    {
        _physics.Work.BeginFrame();
        _physics.Work.Resume();
    }

    /// <summary>Excludes unrelated game processing from the shared physics budget.</summary>
    public void EndSlice() => _physics.Work.Pause();

    /// <summary>Cancels unpublished endpoint preselection without waiting for the worker.</summary>
    public void CancelAnalysis() => _pairAnalysis?.Dispose();

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
        var player = Player.m_localPlayer;
        var center = player ? player.transform.position : bounds.center;
        var cx = Mathf.Clamp(Mathf.RoundToInt(center.x / spacing), x0, x1);
        var cz = Mathf.Clamp(Mathf.RoundToInt(center.z / spacing), z0, z1);
        var radius = Mathf.Max(Mathf.Max(cx - x0, x1 - cx), Mathf.Max(cz - z0, z1 - cz));
        yield return (cx, cz);
        for (var ring = 1; ring <= radius; ring++)
        {
            for (var dx = -ring; dx <= ring; dx++)
            {
                if (cx + dx >= x0 && cx + dx <= x1)
                {
                    if (cz - ring >= z0)
                    {
                        yield return (cx + dx, cz - ring);
                    }

                    if (cz + ring <= z1)
                    {
                        yield return (cx + dx, cz + ring);
                    }
                }
            }

            for (var dz = -ring + 1; dz < ring; dz++)
            {
                if (cz + dz >= z0 && cz + dz <= z1)
                {
                    if (cx - ring >= x0)
                    {
                        yield return (cx - ring, cz + dz);
                    }

                    if (cx + ring <= x1)
                    {
                        yield return (cx + ring, cz + dz);
                    }
                }
            }
        }
    }
}
