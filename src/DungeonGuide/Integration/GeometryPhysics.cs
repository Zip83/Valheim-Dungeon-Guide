using System.Linq;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Isolates Unity collision queries and the solid geometry rules used by dungeon scanning.
/// </summary>
internal sealed class GeometryPhysics
{
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
    /// Returns distinct relevant colliders intersecting a swept passage capsule.
    /// </summary>
    public Collider[] Blockers(Vector3 a, Vector3 b, float radius)
    {
        var d = b - a;
        if (d.magnitude < 0.01f)
        {
            return System.Array.Empty<Collider>();
        }

        return Physics.SphereCastAll(a, radius, d.normalized, d.magnitude, Mask, QueryTriggerInteraction.Ignore)
            .Select(h => h.collider)
            .Where(c => !Excluded(c))
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// Checks that a swept passage is free of relevant solid blockers.
    /// </summary>
    public bool Clear(Vector3 a, Vector3 b, float radius = 0.15f) => Blockers(a, b, radius).Length == 0;

    /// <summary>
    /// Checks that a player-sized standing capsule fits above the sampled floor position.
    /// </summary>
    public bool Standing(Vector3 p) => !Physics.OverlapCapsule(
        p + Vector3.up * 0.4f,
        p + Vector3.up * 1.35f,
        0.23f,
        Mask,
        QueryTriggerInteraction.Ignore)
        .Any(c => !Excluded(c));

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
