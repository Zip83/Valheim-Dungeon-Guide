using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Owns resident floor resources by immutable graph identity and cancels evicted work.</summary>
internal sealed class FloorResourceCache<T> : IDisposable where T : class, IDisposable
{
    private readonly Dictionary<(NavGraph Graph, int Floor), T> _resources = new();
    private readonly List<(NavGraph Graph, int Floor)> _expired = new();

    /// <summary>Number of owned entries, including a temporarily protected displayed resource.</summary>
    public int Count => _resources.Count;

    /// <summary>Owned resources available for nonstructural renderer invalidation.</summary>
    public IEnumerable<T> Resources => _resources.Values;

    /// <summary>Finds an existing floor without recreating its ready image or pending work.</summary>
    public bool TryGet(NavGraph graph, int floor, out T resource) =>
        _resources.TryGetValue((graph, floor), out resource);

    /// <summary>Transfers ownership of a new resource; each key must be added only once.</summary>
    public void Add(NavGraph graph, int floor, T resource) => _resources.Add((graph, floor), resource);

    /// <summary>Retains the current working set and displayed image while disposing all obsolete entries.</summary>
    public void Retain(NavGraph graph, IReadOnlyList<int> floors, T displayed)
    {
        CollectExpired(graph, floors, displayed);
        DisposeExpired();
    }

    private void CollectExpired(NavGraph graph, IReadOnlyList<int> floors, T displayed)
    {
        _expired.Clear();
        foreach (var pair in _resources)
        {
            var desired = ReferenceEquals(pair.Key.Graph, graph) && Contains(floors, pair.Key.Floor);
            if (!desired && !ReferenceEquals(pair.Value, displayed))
            {
                _expired.Add(pair.Key);
            }
        }
    }

    private static bool Contains(IReadOnlyList<int> floors, int floor)
    {
        for (var i = 0; i < floors.Count; i++)
        {
            if (floors[i] == floor)
            {
                return true;
            }
        }

        return false;
    }

    private void DisposeExpired()
    {
        foreach (var key in _expired)
        {
            _resources[key].Dispose();
            _resources.Remove(key);
        }

        _expired.Clear();
    }

    /// <summary>Cancels all pending builds and releases ready resources when the session ends.</summary>
    public void Dispose()
    {
        foreach (var resource in _resources.Values)
        {
            resource.Dispose();
        }

        _resources.Clear();
        _expired.Clear();
    }
}
