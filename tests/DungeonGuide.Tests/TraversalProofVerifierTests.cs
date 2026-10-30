using System;
using System.Collections.Generic;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>
/// Protects directional evidence and revocation while avoiding whole-graph traversal at scan handover.
/// </summary>
public sealed class TraversalProofVerifierTests
{
    /// <summary>
    /// Jump and drop evidence retains the original force, skill, and stamina requirements.
    /// </summary>
    [Theory]
    [InlineData(PassageKind.Jump, 6f, 0.5f, 10f, true)]
    [InlineData(PassageKind.Jump, 5f, 0.5f, 10f, false)]
    [InlineData(PassageKind.Jump, 6f, 0.4f, 10f, false)]
    [InlineData(PassageKind.Jump, 6f, 0.5f, 9f, false)]
    [InlineData(PassageKind.Drop, 6f, 0.5f, 0f, true)]
    [InlineData(PassageKind.Drop, 5f, 0.5f, 10f, false)]
    [InlineData(PassageKind.Drop, 6f, 0.4f, 10f, false)]
    public void RetainsCapabilityRequirements(PassageKind kind, float force, float skill, float stamina, bool expected)
    {
        var proof = Proof();
        var edge = new NavEdge(1, kind, false);
        var verifier = new TraversalProofVerifier();

        verifier.Apply(Index(proof, edge), new[] { proof }, force, skill, stamina, 10);

        Assert.Equal(expected, edge.Verified);
    }

    /// <summary>
    /// Removed evidence revokes a previously verified edge without enumerating every graph edge.
    /// </summary>
    [Fact]
    public void RemovedProofRevokesPreviousVerification()
    {
        var proof = Proof();
        var edge = new NavEdge(1, PassageKind.Jump, false);
        var index = Index(proof, edge);
        var verifier = new TraversalProofVerifier();
        verifier.Apply(index, new[] { proof }, 6, 0.5f, 10, 10);
        Assert.True(edge.Verified);

        verifier.Apply(index, Array.Empty<TraversalProof>(), 6, 0.5f, 10, 10);
        Assert.False(edge.Verified);
    }

    /// <summary>
    /// Replacing a graph revokes old edge references and verifies matching evidence on the new graph only.
    /// </summary>
    [Fact]
    public void ReplacingGraphReleasesOldVerification()
    {
        var proof = Proof();
        var oldEdge = new NavEdge(1, PassageKind.Jump, false);
        var newEdge = new NavEdge(1, PassageKind.Jump, false);
        var verifier = new TraversalProofVerifier();
        verifier.Apply(Index(proof, oldEdge), new[] { proof }, 6, 0.5f, 10, 10);
        Assert.True(oldEdge.Verified);

        verifier.Apply(Index(proof, newEdge), new[] { proof }, 6, 0.5f, 10, 10);
        Assert.False(oldEdge.Verified);
        Assert.True(newEdge.Verified);
    }

    /// <summary>
    /// Evidence for the opposite direction cannot verify a one-way drop.
    /// </summary>
    [Fact]
    public void RejectsReverseDirectionEvidence()
    {
        var forward = Proof();
        var reverse = Proof();
        reverse.from = forward.to;
        reverse.to = forward.from;
        var edge = new NavEdge(1, PassageKind.Drop, false);

        new TraversalProofVerifier().Apply(Index(forward, edge), new[] { reverse }, 6, 0.5f, 10, 10);

        Assert.False(edge.Verified);
    }

    /// <summary>
    /// Duplicate legacy records retain the existing last-record-wins behavior.
    /// </summary>
    [Fact]
    public void LastDuplicateProofWins()
    {
        var valid = Proof();
        var invalid = Proof();
        invalid.jumpForce = 7;
        var edge = new NavEdge(1, PassageKind.Jump, false);
        var index = Index(valid, edge);
        var verifier = new TraversalProofVerifier();

        verifier.Apply(index, new[] { valid, invalid }, 6, 0.5f, 10, 10);
        Assert.False(edge.Verified);

        verifier.Apply(index, new[] { invalid, valid }, 6, 0.5f, 10, 10);
        Assert.True(edge.Verified);
    }

    /// <summary>
    /// Capability loss revokes evidence that was accepted on an earlier update.
    /// </summary>
    [Theory]
    [InlineData(5f, 0.5f, 10f)]
    [InlineData(6f, 0.4f, 10f)]
    [InlineData(6f, 0.5f, 9f)]
    public void RevokesAfterCapabilityLoss(float force, float skill, float stamina)
    {
        var proof = Proof();
        var edge = new NavEdge(1, PassageKind.Jump, false);
        var index = Index(proof, edge);
        var verifier = new TraversalProofVerifier();
        verifier.Apply(index, new[] { proof }, 6, 0.5f, 10, 10);
        Assert.True(edge.Verified);

        verifier.Apply(index, new[] { proof }, force, skill, stamina, 10);
        Assert.False(edge.Verified);
    }

    private static TraversalProof Proof() => new()
    {
        from = new Point(0, 0, 0),
        to = new Point(2, 0, 0),
        jumpForce = 6,
        jumpSkill = 0.5f
    };

    private static Dictionary<(string, string), NavEdge> Index(TraversalProof proof, NavEdge edge) => new()
    {
        [(Point.Key(proof.from), Point.Key(proof.to))] = edge
    };
}
