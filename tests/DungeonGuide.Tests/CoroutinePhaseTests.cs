using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace DungeonGuide.Tests;

/// <summary>Protects suspension timing, exception propagation, and cleanup when composing scan phases.</summary>
public sealed class CoroutinePhaseTests
{
    /// <summary>Phase composition preserves both explicit suspension objects and next-frame null values.</summary>
    [Fact]
    public void PreservesSuspensionValuesAndOrder()
    {
        var marker = new object();
        var phase = Sequence(marker, () => { }, false).GetEnumerator();

        var values = CoroutinePhase.Forward(phase).ToArray();

        Assert.Equal(2, values.Length);
        Assert.Same(marker, values[0]);
        Assert.Null(values[1]);
    }

    /// <summary>A completed scan phase releases its owned iterator exactly once.</summary>
    [Fact]
    public void DisposesCompletedPhase()
    {
        var disposed = 0;
        var phase = Sequence(new object(), () => disposed++, false).GetEnumerator();

        CoroutinePhase.Forward(phase).ToArray();

        Assert.Equal(1, disposed);
    }

    /// <summary>Canceling a suspended scan releases the nested phase immediately.</summary>
    [Fact]
    public void DisposesPhaseOnEarlyCancellation()
    {
        var disposed = 0;
        var phase = Sequence(new object(), () => disposed++, false).GetEnumerator();
        var forwarding = CoroutinePhase.Forward(phase).GetEnumerator();
        Assert.True(forwarding.MoveNext());
        Assert.Equal(0, disposed);

        forwarding.Dispose();

        Assert.Equal(1, disposed);
    }

    /// <summary>Failures reach the scan guard while still releasing the nested iterator.</summary>
    [Fact]
    public void PropagatesFailureAndDisposesPhase()
    {
        var disposed = 0;
        var phase = Sequence(new object(), () => disposed++, true).GetEnumerator();
        using var forwarding = CoroutinePhase.Forward(phase).GetEnumerator();
        Assert.True(forwarding.MoveNext());
        Assert.True(forwarding.MoveNext());

        Assert.Throws<InvalidOperationException>(() => forwarding.MoveNext());

        Assert.Equal(1, disposed);
    }

    private static IEnumerable<object> Sequence(object marker, Action dispose, bool fail)
    {
        try
        {
            yield return marker;
            yield return null;
            if (fail)
            {
                throw new InvalidOperationException("The scan phase failed.");
            }
        }
        finally
        {
            dispose();
        }
    }
}
