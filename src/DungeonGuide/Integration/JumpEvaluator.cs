using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Recognizes short clear geometric jump candidates without treating them as proven traversals.
/// </summary>
internal sealed class JumpEvaluator
{
    private readonly GeometryPhysics _physics;

    /// <summary>
    /// Uses solid-geometry queries to check candidate jump arcs and supported landings.
    /// </summary>
    /// <param name="physics">Shared main-thread standing and arc-clearance queries.</param>
    public JumpEvaluator(GeometryPhysics physics)
    {
        _physics = physics;
    }

    /// <summary>
    /// Rejects out-of-range endpoints, then delegates cooperative landing and arc checks.
    /// Completion is reported exactly once unless enumeration is cancelled.
    /// </summary>
    /// <param name="start">World-space takeoff floor point.</param>
    /// <param name="end">World-space landing floor point.</param>
    /// <param name="work">Budget shared with the other scan phases.</param>
    /// <param name="complete">Receives whether the endpoints and sampled arc form an unverified candidate.</param>
    /// <returns>Frame pauses before budgeted landing and arc queries; enumeration does not verify player
    /// traversal.</returns>
    public IEnumerable<object> Candidate(Vector3 start, Vector3 end, FrameWorkBudget work, Action<bool> complete)
    {
        if (!WithinRange(start, end))
        {
            complete(false);
            yield break;
        }

        foreach (var pause in CheckLandingAndArc(start, end, work, complete))
        {
            yield return pause;
        }
    }

    private static bool WithinRange(Vector3 start, Vector3 end)
    {
        var horizontal = new Vector2(start.x - end.x, start.z - end.z).magnitude;
        var rise = end.y - start.y;
        return JumpCandidateGeometry.WithinRange(horizontal, rise);
    }

    private IEnumerable<object> CheckLandingAndArc(
        Vector3 start, Vector3 end, FrameWorkBudget work, Action<bool> complete)
    {
        while (!work.TryStartWork())
        {
            yield return null;
            work.BeginFrame();
        }

        if (!_physics.Standing(end))
        {
            complete(false);
            yield break;
        }

        foreach (var pause in TraceArc(start, end, work, complete))
        {
            yield return pause;
        }
    }

    private IEnumerable<object> TraceArc(Vector3 start, Vector3 end, FrameWorkBudget work, Action<bool> complete)
    {
        // Geometry is only a candidate: actual traversal is still required before routing uses it.
        var previous = start + Vector3.up * 0.8f;
        for (var i = 1; i <= 12; i++)
        {
            while (!work.TryStartWork())
            {
                yield return null;
                work.BeginFrame();
            }

            var point = ArcPoint(start, end, i / 12f);
            if (!_physics.Clear(previous, point, 0.23f))
            {
                complete(false);
                yield break;
            }

            previous = point;
        }

        complete(true);
    }

    private static Vector3 ArcPoint(Vector3 start, Vector3 end, float progress)
    {
        return Vector3.Lerp(start, end, progress)
            + Vector3.up * JumpCandidateGeometry.ArcLift(progress);
    }
}
