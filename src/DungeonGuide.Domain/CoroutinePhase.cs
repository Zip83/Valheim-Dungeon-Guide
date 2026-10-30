using System;
using System.Collections;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Forwards a phase's suspension points while retaining deterministic iterator cleanup.</summary>
internal static class CoroutinePhase
{
    /// <summary>Exposes a nested iterator without letting the host take over its exception handling.</summary>
    /// <param name="phase">Owned iterator, disposed on completion, failure, or early cancellation.</param>
    /// <returns>The original suspension values in their original order.</returns>
    public static IEnumerable<object> Forward(IEnumerator phase)
    {
        try
        {
            while (phase.MoveNext())
            {
                yield return phase.Current;
            }
        }
        finally
        {
            (phase as IDisposable)?.Dispose();
        }
    }
}
