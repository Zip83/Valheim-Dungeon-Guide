using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Tracks visited floor samples as stable keys that distinguish vertically stacked spaces.
/// </summary>
public sealed class Exploration
{
    private readonly HashSet<string> _known = new();

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

        Revision++;
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

        Revision++;
        return true;
    }

    /// <summary>
    /// Copies known keys for persistence without exposing the mutable exploration set.
    /// </summary>
    /// <returns>A detached list of keys suitable for a save record.</returns>
    public List<string> Export() => new(_known);
}
