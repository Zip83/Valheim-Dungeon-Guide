using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>
/// Owns one floor's sampled coverage and projection without engine or presentation dependencies.
/// </summary>
internal sealed class FloorPlanRaster
{
    /// <summary>Creates a square coverage grid; zero owners represent unsampled space.</summary>
    public FloorPlanRaster(int size, int floor, Point center, float span)
    {
        if (size < 2 || !(span > 0))
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        Size = size;
        Floor = floor;
        Center = center;
        Span = span;
        Owners = new int[size * size];
        Boundary = new bool[size * size];
    }

    /// <summary>Published scan that owns all node indices in this raster.</summary>
    public NavGraph Graph
    {
        get; internal set;
    }

    /// <summary>Resolution of each side in pixels.</summary>
    public int Size
    {
        get;
    }

    /// <summary>Architectural floor represented by this grid.</summary>
    public int Floor
    {
        get;
    }

    /// <summary>World-space center of the complete map.</summary>
    public Point Center
    {
        get;
    }

    /// <summary>World-space width of the complete map in metres.</summary>
    public float Span
    {
        get;
    }

    /// <summary>Node index plus one for each covered pixel; zero indicates a geometric gap.</summary>
    public int[] Owners
    {
        get;
    }

    /// <summary>True on the conservative edge of sampled coverage.</summary>
    public bool[] Boundary
    {
        get;
    }

    /// <summary>Candidate transition and special-passage nodes, indexed once during construction.</summary>
    public List<int> Features { get; } = new();

    /// <summary>Connected stair regions prepared once so dynamic markers need not draw every vertical sample.</summary>
    public List<StairMarkerGroup> StairGroups { get; } = new();

    /// <summary>Verified ordinary connections used to distinguish internal cell seams from walls.</summary>
    public HashSet<(int, int)> Connections { get; } = new();

    /// <summary>Projects world X to a continuous raster coordinate.</summary>
    public float X(float x) => ((x - Center.x) / Span + 0.5f) * Size;

    /// <summary>Projects world Z to a continuous raster coordinate.</summary>
    public float Y(float z) => ((z - Center.z) / Span + 0.5f) * Size;

    /// <summary>Returns the owning node index, or -1 outside sampled coverage.</summary>
    public int NodeAt(Point point)
    {
        var x = (int)Math.Floor(X(point.x));
        var y = (int)Math.Floor(Y(point.z));
        return x < 0 || y < 0 || x >= Size || y >= Size ? -1 : Owners[y * Size + x] - 1;
    }
}
