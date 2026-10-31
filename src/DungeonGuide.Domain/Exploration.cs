using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Tracks visited floor samples as stable keys that distinguish vertically stacked spaces.
/// </summary>
public sealed class Exploration
{
    private readonly HashSet<string> _known = new();
    private readonly Dictionary<int, int> _floorRevisions = new();

    /// <summary>Monotonic additions since the latest load, independently consumed by each map layer.</summary>
    internal List<(string Key, int Floor, long Timestamp)> Changes { get; } = new();

    /// <summary>Changes when replacing the underlying saved keys, invalidating incremental consumers.</summary>
    internal int Generation
    {
        get; private set;
    }

    /// <summary>Returns the revision for a single physical floor, including unclassified additions.</summary>
    internal int FloorRevision(int floor) =>
        (_floorRevisions.TryGetValue(floor, out var revision) ? revision : 0)
        + (_floorRevisions.TryGetValue(-1, out var unknown) ? unknown : 0);

    private void Changed(string key, int floor)
    {
        Changes.Add((key, floor, System.Diagnostics.Stopwatch.GetTimestamp()));
        _floorRevisions[floor] = (_floorRevisions.TryGetValue(floor, out var revision) ? revision : 0) + 1;
        Revision++;
    }

    /// <summary>
    /// Change counter used to invalidate rendered exploration caches.
    /// </summary>
    public int Revision
    {
        get; private set;
    }

    /// <summary>
    /// Replaces known samples with saved keys and advances the change counter.
    /// </summary>
    /// <param name="keys">Previously saved exploration keys to load.</param>
    public void Load(IEnumerable<string> keys)
    {
        _known.Clear();
        Changes.Clear();
        _floorRevisions.Clear();
        Generation++;
        foreach (var key in keys)
        {
            _known.Add(key);
        }

        Revision++;
    }

    /// <summary>
    /// Reports whether the sample has already been revealed at its quantized world-space position.
    /// </summary>
    /// <param name="p">World-space position measured in metres.</param>
    /// <returns>True when the sample position has already been revealed.</returns>
    public bool Has(Point p) => _known.Contains(Point.Key(p));

    /// <summary>
    /// Reports whether the sample has already been revealed at its quantized world-space position.
    /// </summary>
    /// <param name="node">Sampled floor node whose position and stable key identify exploration.</param>
    /// <returns>True when the sample position has already been revealed.</returns>
    public bool Has(NavNode node) => _known.Contains(node.Key);

    /// <summary>
    /// Adds a previously unknown sample and advances the revision only when exploration changes.
    /// </summary>
    /// <param name="node">Sampled floor node whose position and stable key identify exploration.</param>
    /// <returns>True only when the sample was previously unknown.</returns>
    public bool Reveal(NavNode node)
    {
        if (!_known.Add(node.Key))
        {
            return false;
        }

        Changed(node.Key, node.Floor);
        return true;
    }

    /// <summary>
    /// Adds a previously unknown sample and advances the revision only when exploration changes.
    /// </summary>
    /// <param name="p">World-space position measured in metres.</param>
    /// <returns>True only when the sample was previously unknown.</returns>
    public bool Reveal(Point p)
    {
        if (!_known.Add(Point.Key(p)))
        {
            return false;
        }

        Changed(Point.Key(p), -1);
        return true;
    }

    /// <summary>
    /// Copies known keys for persistence without exposing the mutable exploration set.
    /// </summary>
    /// <returns>A detached list of keys suitable for a save record.</returns>
    public List<string> Export() => new(_known);
}
