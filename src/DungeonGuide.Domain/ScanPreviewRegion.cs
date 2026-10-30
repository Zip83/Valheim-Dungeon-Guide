using System;

namespace DungeonGuide;

/// <summary>Limits the initial scan to a neighborhood around the player's current elevation.</summary>
public sealed class ScanPreviewRegion
{
    /// <summary>Lower corner of the preview, clipped to the original dungeon bounds.</summary>
    public Point Minimum
    {
        get;
    }

    /// <summary>Upper corner of the preview, clipped to the original dungeon bounds.</summary>
    public Point Maximum
    {
        get;
    }

    private ScanPreviewRegion(Point minimum, Point maximum)
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>Creates a 32 m wide, 8 m high preview without assigning architectural floor numbers.</summary>
    /// <param name="minimum">Lower corner of the complete dungeon.</param>
    /// <param name="maximum">Upper corner of the complete dungeon.</param>
    /// <param name="player">Player position at scan start.</param>
    /// <returns>Clipped preview, or null when the player is outside the dungeon bounds.</returns>
    public static ScanPreviewRegion Create(Point minimum, Point maximum, Point player)
    {
        if (!Contains(minimum, maximum, player))
        {
            return null;
        }

        return new ScanPreviewRegion(
            new Point(Math.Max(minimum.x, player.x - 16), Math.Max(minimum.y, player.y - 4),
                Math.Max(minimum.z, player.z - 16)),
            new Point(Math.Min(maximum.x, player.x + 16), Math.Min(maximum.y, player.y + 4),
                Math.Min(maximum.z, player.z + 16)));
    }

    private static bool Contains(Point minimum, Point maximum, Point player)
    {
        return player.x >= minimum.x && player.x <= maximum.x
            && player.y >= minimum.y && player.y <= maximum.y
            && player.z >= minimum.z && player.z <= maximum.z;
    }
}
