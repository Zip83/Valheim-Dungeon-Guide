using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Checks a conservative straight pull to a supported landing and a static attachable surface beyond it.</summary>
internal sealed class GrappleLandingProbe
{
    private readonly GeometryPhysics _physics;

    /// <summary>Shares the scan/exploration physics budget and collision rules.</summary>
    public GrappleLandingProbe(GeometryPhysics physics)
    {
        _physics = physics;
    }

    /// <summary>Reports a checked anchor or null; physics remains on the main thread and yields between queries.</summary>
    public IEnumerable<object> Check(Vector3 from, Vector3 landing, GrappleEquipment hook, Action<Vector3?> complete)
    {
        var eye = hook.ShotOrigin(from, landing);
        var body = from + Vector3.up * 1.2f;
        var arrival = landing + Vector3.up * 1.2f;
        if (Vector3.Distance(eye, arrival) > hook.Range)
        {
            complete(null);
            yield break;
        }

        var supported = false;
        foreach (var pause in CheckLanding(landing, valid => supported = valid))
        {
            yield return pause;
        }

        if (!supported)
        {
            complete(null);
            yield break;
        }

        var clear = false;
        foreach (var pause in CheckApproach(body, eye, arrival, hook.Radius, valid => clear = valid))
        {
            yield return pause;
        }

        if (!clear)
        {
            complete(null);
            yield break;
        }

        foreach (var pause in FindAnchor(eye, arrival, hook, complete))
        {
            yield return pause;
        }
    }

    private IEnumerable<object> CheckLanding(Vector3 landing, Action<bool> complete)
    {
        foreach (var pause in Reserve())
        {
            yield return pause;
        }

        if (!_physics.Standing(landing))
        {
            complete(false);
            yield break;
        }

        foreach (var pause in Reserve())
        {
            yield return pause;
        }

        complete(_physics.Supported(landing));
    }

    private IEnumerable<object> CheckApproach(Vector3 body, Vector3 eye, Vector3 arrival, float shotRadius,
        Action<bool> complete)
    {
        foreach (var pause in Reserve())
        {
            yield return pause;
        }

        if (!_physics.Clear(body, arrival, 0.3f))
        {
            complete(false);
            yield break;
        }

        foreach (var pause in Reserve())
        {
            yield return pause;
        }

        complete(_physics.Clear(body, eye, shotRadius));
    }

    private IEnumerable<object> FindAnchor(Vector3 eye, Vector3 arrival, GrappleEquipment hook,
        Action<Vector3?> complete)
    {
        foreach (var pause in Reserve())
        {
            yield return pause;
        }

        var delta = arrival - eye;
        var length = Mathf.Min(hook.Range, delta.magnitude + 4);
        if (delta.sqrMagnitude > 0.01f && Physics.Raycast(eye, delta.normalized, out var hit, length,
            _physics.Mask, QueryTriggerInteraction.Ignore) && CanAttach(hit.collider, hook)
            && Vector3.Distance(hit.point, arrival) <= 4 && hit.distance >= delta.magnitude - 0.1f)
        {
            foreach (var pause in CheckShot(eye, hit.point, hook, complete))
            {
                yield return pause;
            }
        }
        else
        {
            complete(null);
        }
    }

    private IEnumerable<object> CheckShot(Vector3 origin, Vector3 anchor, GrappleEquipment hook,
        Action<Vector3?> complete)
    {
        if (!GrappleShot.TryCreate(UnityPoints.P(anchor - origin), hook.Speed, hook.Gravity, hook.Lifetime,
            out var shot))
        {
            complete(null);
            yield break;
        }

        var steps = Mathf.Max(8, Mathf.CeilToInt(Vector3.Distance(origin, anchor) * 2));
        var previous = origin;
        for (var i = 1; i <= steps; i++)
        {
            foreach (var pause in Reserve())
            {
                yield return pause;
            }

            var point = origin + UnityPoints.V(shot.At((float)i / steps));
            if (i == steps)
            {
                point -= (point - previous).normalized * (hook.Radius + 0.1f);
            }

            if (!_physics.Clear(previous, point, hook.Radius))
            {
                complete(null);
                yield break;
            }

            previous = point;
        }

        complete(anchor);
    }

    private static bool CanAttach(Collider collider, GrappleEquipment hook)
    {
        return collider && !collider.attachedRigidbody && !collider.GetComponentInParent<Door>()
            && !collider.GetComponentInParent<Character>() && !collider.GetComponentInParent<GrapplingBlocker>()
            && hook.Attachment.CanAttach(collider.gameObject);
    }

    private IEnumerable<object> Reserve()
    {
        while (!_physics.Work.TryStartWork())
        {
            yield return null;
            _physics.Work.BeginFrame();
        }
    }
}
