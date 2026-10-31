using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Keeps distant visibility work progressing while player movement refreshes nearby candidates.</summary>
internal sealed class ExplorationSweep : IDisposable
{
    private IEnumerator<NavNode> _nearby;
    private IEnumerator<NavNode> _distant;
    private bool _preferNearby = true;

    /// <summary>Whether a radius sweep is still pending, independently of nearby movement.</summary>
    public bool HasDistant => _distant != null;

    /// <summary>Replaces immediate surroundings without discarding progress through the wider radius.</summary>
    public void RefreshNearby(IEnumerable<NavNode> candidates)
    {
        _nearby?.Dispose();
        _nearby = candidates.GetEnumerator();
    }

    /// <summary>Starts a radius sweep after completion or a geometry/floor reset.</summary>
    public void RefreshDistant(IEnumerable<NavNode> candidates)
    {
        _distant?.Dispose();
        _distant = candidates.GetEnumerator();
    }

    /// <summary>Alternates nearby and distant work so neither movement nor a large radius can starve the other.</summary>
    public bool TryTake(out NavNode node)
    {
        var nearby = _preferNearby;
        _preferNearby = !nearby;
        return nearby
            ? Advance(ref _nearby, out node) || Advance(ref _distant, out node)
            : Advance(ref _distant, out node) || Advance(ref _nearby, out node);
    }

    private static bool Advance(ref IEnumerator<NavNode> candidates, out NavNode node)
    {
        node = null;
        if (candidates == null)
        {
            return false;
        }

        if (candidates.MoveNext())
        {
            node = candidates.Current;
            return true;
        }

        candidates.Dispose();
        candidates = null;
        return false;
    }

    /// <summary>Cancels both streams when geometry, physical floor or session changes.</summary>
    public void Reset()
    {
        _nearby?.Dispose();
        _distant?.Dispose();
        _nearby = _distant = null;
        _preferNearby = true;
    }

    /// <inheritdoc />
    public void Dispose() => Reset();
}
