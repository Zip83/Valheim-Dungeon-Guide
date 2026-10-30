using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Records actual grounded takeoff and landing evidence for directional special passages.
/// </summary>
internal sealed class TraversalRecorder
{
    private readonly GraphLocator _locator;
    private readonly TraversalProofVerifier _verifier;
    private Point _grounded;
    private bool _hasGround;
    private bool _airborne;
    private float _began;
    private float _nextApply;
    private readonly FieldInfo _debugFly = typeof(Player).GetField(
        "m_debugFly",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    /// <summary>
    /// Uses supported graph snapping to match observed takeoff and landing positions.
    /// </summary>
    public TraversalRecorder(GraphLocator locator, TraversalProofVerifier verifier)
    {
        _locator = locator;
        _verifier = verifier;
    }

    /// <summary>
    /// Clears temporary takeoff/landing observation when the active dungeon changes.
    /// </summary>
    public void Reset()
    {
        _hasGround = _airborne = false;
        _verifier.Reset();
    }

    /// <summary>
    /// Observes actual grounded traversal, excluding debug flight and implausible movement.
    /// </summary>
    public void Tick(DungeonSession session, Player player)
    {
        if (!CanObserve(session, player))
        {
            Reset();
            return;
        }

        TrackMovement(session, player);
        if (Time.unscaledTime >= _nextApply)
        {
            _nextApply = Time.unscaledTime + 0.5f;
            Apply(session, player);
        }
    }

    /// <summary>
    /// Revalidates saved directional traversal evidence against current player capabilities and scan geometry.
    /// </summary>
    public void Apply(DungeonSession session, Player player)
    {
        if (session.Scan == null)
        {
            return;
        }

        _verifier.Apply(
            session.Scan.TraversalCandidates,
            session.Data.traversals,
            player.m_jumpForce,
            player.GetSkillFactor(Skills.SkillType.Jump),
            player.GetStamina(),
            player.m_jumpStaminaUsage);
    }

    private bool CanObserve(DungeonSession session, Player player)
    {
        return session.Scan != null && !player.IsTeleporting() && !player.IsDead() && _debugFly != null
            && !(bool)_debugFly.GetValue(player);
    }

    private void TrackMovement(DungeonSession session, Player player)
    {
        var ground = player.IsOnGround();
        var p = UnityPoints.P(player.transform.position);
        if (!ground && _hasGround && !_airborne)
        {
            _airborne = true;
            _began = Time.unscaledTime;
        }

        if (ground)
        {
            if (_airborne && Time.unscaledTime - _began < 3)
            {
                RecordLanding(session, player);
            }

            _airborne = false;
            _hasGround = true;
            _grounded = p;
        }
    }

    private void RecordLanding(DungeonSession session, Player player)
    {
        var a = _locator.Snap(session.Scan, UnityPoints.V(_grounded));
        var b = _locator.Snap(session.Scan, player.transform.position);
        if (a >= 0 && b >= 0)
        {
            var edge = session.Scan.Graph.Nodes[a].Edges.FirstOrDefault(
                e => e.To == b && (e.Kind == PassageKind.Jump || e.Kind == PassageKind.Drop));
            if (edge != null)
            {
                SaveProof(session, player, a, b);
            }
        }
    }

    private static void SaveProof(DungeonSession session, Player player, int a, int b)
    {
        var from = session.Scan.Graph.Nodes[a].Position;
        var to = session.Scan.Graph.Nodes[b].Position;
        session.Data.traversals.RemoveAll(t => Point.Key(t.from) == Point.Key(from) && Point.Key(t.to) == Point.Key(to));
        session.Data.traversals.Add(new TraversalProof
        {
            from = from,
            to = to,
            jumpForce = player.m_jumpForce,
            jumpSkill = player.GetSkillFactor(Skills.SkillType.Jump)
        });
    }
}
