using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Retains a small number of recent live dungeon sessions while every geometry file remains on disk.</summary>
internal sealed class DungeonSessionCache
{
    private const int Capacity = 3;
    private readonly Dictionary<string, DungeonSession> _sessions = new();
    private readonly LinkedList<string> _recent = new();

    /// <summary>Returns the exact prior session so exploration and prepared floor textures can be reused.</summary>
    public DungeonSession Find(string key)
    {
        if (!_sessions.TryGetValue(key, out var session))
        {
            return null;
        }

        _recent.Remove(key);
        _recent.AddLast(key);
        return session;
    }

    /// <summary>Keeps the active/recent graph without retaining every visited dungeon indefinitely in memory.</summary>
    public void Remember(string key, DungeonSession session)
    {
        _sessions[key] = session;
        _recent.Remove(key);
        _recent.AddLast(key);
        while (_sessions.Count > Capacity)
        {
            var oldest = _recent.First.Value;
            _recent.RemoveFirst();
            _sessions.Remove(oldest);
        }
    }

    /// <summary>Removes a layout whose fingerprint has changed during the active session.</summary>
    public void Remove(string key)
    {
        _sessions.Remove(key);
        _recent.Remove(key);
    }

    /// <summary>Releases all previous-world sessions when the world or connection changes.</summary>
    public void Clear()
    {
        _sessions.Clear();
        _recent.Clear();
    }
}
