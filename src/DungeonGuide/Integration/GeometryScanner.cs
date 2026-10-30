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

    /// <summary>
    /// Composes the sampling and linking phases using shared main-thread physics services.
    /// </summary>
    public GeometryScanner(GeometryPhysics physics, PassageEvaluator passages, JumpEvaluator jumps)
    {
        _physics = physics;
        _sampler = new ScanColumnSampler(physics);
        _linker = new ScanPassageLinker(passages, new ScanGapLinker(physics, passages, jumps));
    }

    /// <summary>
    /// Defers physics until a later frame, then publishes the completed graph and completeness metadata.
    /// </summary>
    public IEnumerator Scan(
        DungeonArea area, float spacing, int limit, int stepsPerFrame,
        float millisecondsPerFrame, Action<ScanResult> complete)
    {
        // StartCoroutine runs until the first yield; do no physics work in the input handler.
        yield return null;
        var work = new FrameWorkBudget(stepsPerFrame, millisecondsPerFrame);
        var overflowRevision = _physics.BufferOverflows;
        var result = new ScanResult { Spacing = spacing };

        foreach (var pause in SampleColumns(area, result, limit, work))
        {
            yield return pause;
        }

        foreach (var pause in _linker.Link(result, work))
        {
            yield return pause;
        }

        result.Truncated |= _physics.BufferOverflows != overflowRevision;
        complete(result);
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
