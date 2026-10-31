using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Records actual grounded takeoff and landing evidence for directional special passages.
/// </summary>
internal sealed class TraversalRecorder
{
    private readonly ObservedTraversalReplay _replay;
    private readonly TraversalProofVerifier _verifier;
    private Point _grounded;
    private bool _hasGround;
    private bool _airborne;
    private float _began;
    private float _jumpForce;
    private float _jumpSkill;
    private float _nextApply;
    private readonly FieldInfo _debugFly = typeof(Player).GetField(
        "m_debugFly",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    /// <summary>
    /// Records physical movement independently of automatic scan candidates and replays saved evidence.
    /// </summary>
    public TraversalRecorder(ObservedTraversalReplay replay, TraversalProofVerifier verifier)
    {
        _replay = replay;
        _verifier = verifier;
    }

    /// <summary>
    /// Clears temporary takeoff/landing observation when the active dungeon changes.
    /// </summary>
    public void Reset()
    {
        _hasGround = _airborne = false;
        _verifier.Reset();
        _replay.Reset();
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
        _replay.Tick(session.Scan, session.Data.traversals);
        if (!_replay.Ready)
        {
            _verifier.Reset();
        }
        else if (Time.unscaledTime >= _nextApply)
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
        return session.Scan != null && !player.IsTeleporting() && !player.IsDead() && player.m_grappling <= 0
            && !GrapplingPoint.m_localGrappler && _debugFly != null
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
            _jumpForce = player.m_jumpForce;
            _jumpSkill = player.GetSkillFactor(Skills.SkillType.Jump);
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
        var landing = UnityPoints.P(player.transform.position);
        var distance = Point.Distance(_grounded, landing);
        if (distance < 0.75f || distance > 30)
        {
            return;
        }

        session.Data.traversals.Add(new TraversalProof
        {
            from = _grounded,
            to = landing,
            jumpForce = _jumpForce,
            jumpSkill = _jumpSkill
        });
        _replay.Reset();
    }
}
