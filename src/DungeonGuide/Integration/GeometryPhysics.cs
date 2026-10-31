using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Isolates Unity collision queries and the solid geometry rules used by dungeon scanning.
/// </summary>
internal sealed class GeometryPhysics
{
    /// <summary>Shared soft physics budget for scanning and exploration in the current game frame.</summary>
    public FrameWorkBudget Work { get; private set; } = new(4096, 1, frame: () => Time.frameCount);

    /// <summary>Applies the configured scan limits to the shared physics scheduler.</summary>
    public void ConfigureBudget(int steps, float milliseconds)
    {
        Work = new FrameWorkBudget(steps, milliseconds, frame: () => Time.frameCount);
    }

    private readonly RaycastHit[] _passageHits = new RaycastHit[128];
    private readonly RaycastHit[] _clearanceHits = new RaycastHit[128];
    private readonly Collider[] _standingHits = new Collider[128];

    /// <summary>
    /// Count of saturated query buffers, used to report potentially incomplete scans conservatively.
    /// </summary>
    public long BufferOverflows
    {
        get; private set;
    }

    /// <summary>
    /// Unity solid-geometry layers considered by scanning and passage verification.
    /// </summary>
    public int Mask { get; } = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain");

    /// <summary>
    /// Reports colliders that should not count as static floor or passage blockers.
    /// </summary>
    public bool Excluded(Collider c) =>
        !c || c.isTrigger || c.GetComponentInParent<Character>() || c.GetComponentInParent<Container>();

    /// <summary>
    /// Appends distinct relevant passage blockers into caller-owned reusable storage; false means query overflow.
    /// </summary>
    public bool AddBlockers(Vector3 a, Vector3 b, float radius, List<Collider> blockers)
    {
        var delta = b - a;
        if (delta.magnitude < 0.01f)
        {
            return true;
        }

        var count = Physics.SphereCastNonAlloc(
            a,
            radius,
            delta.normalized,
            _passageHits,
            delta.magnitude,
            Mask,
            QueryTriggerInteraction.Ignore);
        if (count >= _passageHits.Length)
        {
            BufferOverflows++;
            return false;
        }

        for (var i = 0; i < count; i++)
        {
            var collider = _passageHits[i].collider;
            if (!Excluded(collider) && !blockers.Contains(collider))
            {
                blockers.Add(collider);
            }
        }

        return true;
    }

    /// <summary>
    /// Checks that a swept passage is free of relevant solid blockers.
    /// </summary>
    public bool Clear(Vector3 a, Vector3 b, float radius = 0.15f)
    {
        var delta = b - a;
        if (delta.magnitude < 0.01f)
        {
            return true;
        }

        var count = Physics.SphereCastNonAlloc(
            a,
            radius,
            delta.normalized,
            _clearanceHits,
            delta.magnitude,
            Mask,
            QueryTriggerInteraction.Ignore);
        if (count >= _clearanceHits.Length)
        {
            BufferOverflows++;
            return false;
        }

        for (var i = 0; i < count; i++)
        {
            if (!Excluded(_clearanceHits[i].collider))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks that a standing capsule fits, treating a full query buffer as unverified clearance.
    /// </summary>
    public bool Standing(Vector3 p)
    {
        var count = Physics.OverlapCapsuleNonAlloc(
            p + Vector3.up * 0.4f,
            p + Vector3.up * 1.35f,
            0.23f,
            _standingHits,
            Mask,
            QueryTriggerInteraction.Ignore);
        if (count >= _standingHits.Length)
        {
            BufferOverflows++;
            return false;
        }

        for (var i = 0; i < count; i++)
        {
            if (!Excluded(_standingHits[i]))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks for a sufficiently level nearby floor underneath the sample.
    /// </summary>
    public bool Supported(Vector3 p) => Physics.Raycast(
        p + Vector3.up * 0.65f,
        Vector3.down,
        out var hit,
        1.25f,
        Mask,
        QueryTriggerInteraction.Ignore)
        && !Excluded(hit.collider)
        && hit.normal.y > 0.55f
        && Mathf.Abs(hit.point.y - p.y) < 0.65f;

    /// <summary>
    /// Checks an elevated line of sight between floor samples for exploration revealing.
    /// </summary>
    public bool Visible(Vector3 a, Vector3 b) => Clear(a + Vector3.up * 0.65f, b + Vector3.up * 0.65f, 0.1f);
}
