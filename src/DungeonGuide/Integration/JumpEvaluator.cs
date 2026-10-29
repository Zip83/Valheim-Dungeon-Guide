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
    public JumpEvaluator(GeometryPhysics physics)
    {
        _physics = physics;
    }

    /// <summary>
    /// Checks takeoff/landing geometry, yielding between arc queries without treating an unobserved jump as verified.
    /// </summary>
    public IEnumerable<object> Candidate(Vector3 a, Vector3 b, FrameWorkBudget work, Action<bool> complete)
    {
        var h = new Vector2(a.x - b.x, a.z - b.z).magnitude;
        var dy = b.y - a.y;
        if (h < 1.5f || h > 2.8f || dy > 0.9f || dy < -2f)
        {
            complete(false);
            yield break;
        }

        while (!work.TryStartWork())
        {
            yield return null;
            work.BeginFrame();
        }

        if (!_physics.Standing(b))
        {
            complete(false);
            yield break;
        }

        // Candidate only: actual traversal is required before this enters an automatic route.
        var last = a + Vector3.up * 0.8f;
        for (var i = 1; i <= 12; i++)
        {
            while (!work.TryStartWork())
            {
                yield return null;
                work.BeginFrame();
            }

            var t = i / 12f;
            var p = Vector3.Lerp(a, b, t) + Vector3.up * (0.8f + 4 * t * (1 - t) * 0.9f);
            if (!_physics.Clear(last, p, 0.23f))
            {
                complete(false);
                yield break;
            }

            last = p;
        }

        complete(true);
    }
}
