namespace DungeonGuide;

/// <summary>Owns a reusable dungeon graph and floor layout independently of live Unity objects.</summary>
internal sealed class CachedGeometry
{
    /// <summary>Sampled positions and directed passages.</summary>
    public NavGraph Graph
    {
        get;
    }

    /// <summary>Architectural floor elevations and supported destination nodes.</summary>
    public FloorLayout Layout
    {
        get;
    }

    /// <summary>Sampling distance in metres.</summary>
    public float Spacing;

    /// <summary>Whether the scan reached an operation or physics-buffer limit.</summary>
    public bool Truncated;

    /// <summary>Whether complete navigation analysis is available rather than preview geometry only.</summary>
    public bool Complete;

    /// <summary>Confirmed native doorway arrival, when observed during entry.</summary>
    public Point? Entrance;

    /// <summary>Wraps exclusively owned or game-thread-retained published geometry without copying live Unity objects.</summary>
    public CachedGeometry(NavGraph graph = null, FloorLayout layout = null)
    {
        Graph = graph ?? new NavGraph();
        Layout = layout ?? new FloorLayout();
    }

}
