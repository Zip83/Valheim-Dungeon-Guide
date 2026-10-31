using System.Collections.Generic;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects visibility progress under continuous movement and cooperative frame limits.</summary>
public sealed class ExplorationSweepTests
{
    /// <summary>Refreshing immediate surroundings every step cannot postpone distant floor indefinitely.</summary>
    [Fact]
    public void MovementDoesNotRestartDistantSweep()
    {
        using var sweep = new ExplorationSweep();
        var distant = new NavNode(new Point(25, 0, 0));
        sweep.RefreshDistant(new[] { null, null, distant });
        var exploration = new Exploration();
        for (var frame = 0; frame < 10 && !exploration.Has(distant); frame++)
        {
            sweep.RefreshNearby(new[] { new NavNode(new Point(frame, 0, 1)) });
            Assert.True(sweep.TryTake(out var node));
            if (node != null)
            {
                ExplorationRevealPolicy.TryReveal(exploration, node, new Point(), 0, (_, _) => true);
            }
        }

        Assert.True(exploration.Has(distant));
    }

    /// <summary>Immediate surroundings receive work even during a long sweep through empty columns.</summary>
    [Fact]
    public void NearbyMovementIsNotBlockedByDistantSweep()
    {
        using var sweep = new ExplorationSweep();
        sweep.RefreshDistant(new NavNode[100]);
        Assert.True(sweep.TryTake(out _));
        var nearby = new NavNode(new Point(1, 0, 0));
        sweep.RefreshNearby(new[] { nearby });
        var taken = new List<NavNode>();
        for (var step = 0; step < 2; step++)
        {
            Assert.True(sweep.TryTake(out var node));
            taken.Add(node);
        }

        Assert.Contains(nearby, taken);
    }

    /// <summary>A floor or geometry reset cannot yield candidates retained from the previous graph.</summary>
    [Fact]
    public void ResetDiscardsBothStreams()
    {
        using var sweep = new ExplorationSweep();
        sweep.RefreshNearby(new[] { new NavNode(new Point()) });
        sweep.RefreshDistant(new[] { new NavNode(new Point(20, 0, 0)) });
        sweep.Reset();

        Assert.False(sweep.HasDistant);
        Assert.False(sweep.TryTake(out _));
    }

    /// <summary>Finishing a sweep permits blocked visibility to be checked again after a door opens.</summary>
    [Fact]
    public void CompletedSweepCanRetryBlockedSample()
    {
        using var sweep = new ExplorationSweep();
        var sample = new NavNode(new Point(20, 0, 0));
        var exploration = new Exploration();
        sweep.RefreshDistant(new[] { sample });
        Assert.True(sweep.TryTake(out var blocked));
        Assert.False(ExplorationRevealPolicy.TryReveal(exploration, blocked, new Point(), 0, (_, _) => false));
        Assert.False(sweep.TryTake(out _));
        Assert.False(sweep.HasDistant);
        sweep.RefreshDistant(new[] { sample });
        Assert.True(sweep.TryTake(out var visible));

        Assert.True(ExplorationRevealPolicy.TryReveal(exploration, visible, new Point(), 0, (_, _) => true));
    }
}
