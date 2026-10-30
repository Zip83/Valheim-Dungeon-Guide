using System;

namespace DungeonGuide;

/// <summary>Stores the visible extent of one overview floor without exposing undiscovered sample bounds.</summary>
internal sealed class OverviewFloor
{
    /// <summary>Index into the active scan's ascending floor list.</summary>
    public int Floor
    {
        get;
    }

    /// <summary>Lower horizontal corner of the samples permitted by the selected reveal mode.</summary>
    public Point Minimum
    {
        get; private set;
    }

    /// <summary>Upper horizontal corner of the samples permitted by the selected reveal mode.</summary>
    public Point Maximum
    {
        get; private set;
    }

    /// <summary>Starts an extent from the first visible sample of a floor.</summary>
    public OverviewFloor(int floor, Point position)
    {
        Floor = floor;
        Minimum = Maximum = position;
    }

    /// <summary>Expands this floor's extent to include another visible sample.</summary>
    public void Include(Point position)
    {
        Minimum = new Point(Math.Min(Minimum.x, position.x), Minimum.y, Math.Min(Minimum.z, position.z));
        Maximum = new Point(Math.Max(Maximum.x, position.x), Maximum.y, Math.Max(Maximum.z, position.z));
    }
}
