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
    private Point _grounded;
    private bool _hasGround;
    private bool _airborne;
    private float _began;
    private float _nextApply;
    private readonly FieldInfo _debugFly = typeof(Player).GetField(
        "m_debugFly",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private ScanResult _cachedScan;
    private List<(NavNode node, NavEdge edge)> _candidates = new();

    /// <summary>
    /// Uses supported graph snapping to match observed takeoff and landing positions.
    /// </summary>
    public TraversalRecorder(GraphLocator locator)
    {
        _locator = locator;
    }

    /// <summary>
    /// Clears temporary takeoff/landing observation when the active dungeon changes.
    /// </summary>
    public void Reset()
    {
        _hasGround = _airborne = false;
    }

    /// <summary>
    /// Observes actual grounded traversal, excluding debug flight and implausible movement.
    /// </summary>
    public void Tick(DungeonSession session, Player player)
    {
        if (session.Scan == null || player.IsTeleporting() || player.IsDead() || _debugFly == null
            || (bool)_debugFly.GetValue(player))
        {
            Reset();
            return;
        }

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
                var a = _locator.Snap(session.Scan, UnityPoints.V(_grounded));
                var b = _locator.Snap(session.Scan, player.transform.position);
                if (a >= 0 && b >= 0)
                {
                    var edge = session.Scan.Graph.Nodes[a].Edges.FirstOrDefault(
                        e => e.To == b && (e.Kind == PassageKind.Jump || e.Kind == PassageKind.Drop));
                    if (edge != null)
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
            }

            _airborne = false;
            _hasGround = true;
            _grounded = p;
        }

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

        // Proof does not survive reduced jump capability. Current stamina is also required for jumps.
        if (_cachedScan != session.Scan)
        {
            _cachedScan = session.Scan;
            _candidates = session.Scan.Graph.Nodes
                .SelectMany(n => n.Edges
                    .Where(e => e.Kind == PassageKind.Jump || e.Kind == PassageKind.Drop)
                    .Select(e => (n, e)))
                .ToList();
        }

        var skill = player.GetSkillFactor(Skills.SkillType.Jump);
        var force = player.m_jumpForce;
        var stamina = player.GetStamina();
        var proofs = new Dictionary<(string, string), TraversalProof>();
        foreach (var proof in session.Data.traversals)
        {
            proofs[(Point.Key(proof.from), Point.Key(proof.to))] = proof;
        }

        foreach (var candidate in _candidates)
        {
            var node = candidate.node;
            var edge = candidate.edge;
            var to = session.Scan.Graph.Nodes[edge.To];
            edge.Verified = proofs.TryGetValue((node.Key, to.Key), out var proof) && proof.jumpForce <= force + 0.01f
                && proof.jumpSkill <= skill + 0.01f
                && (edge.Kind == PassageKind.Drop || stamina >= player.m_jumpStaminaUsage);
        }
    }
}
