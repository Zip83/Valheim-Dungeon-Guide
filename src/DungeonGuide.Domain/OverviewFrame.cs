using System;

namespace DungeonGuide;

/// <summary>Preserves discovered-only framing while reusing a ready full-floor image.</summary>
internal readonly struct OverviewFrame
{
    /// <summary>Creates the same square projection and sample padding as an overview raster.</summary>
    public OverviewFrame(OverviewFloor floor, float spacing)
    {
        Floor = floor.Floor;
        Center = new Point((floor.Minimum.x + floor.Maximum.x) / 2, 0,
            (floor.Minimum.z + floor.Maximum.z) / 2);
        Span = Math.Max(floor.Maximum.x - floor.Minimum.x, floor.Maximum.z - floor.Minimum.z) + spacing * 3;
    }

    /// <summary>Floor index scoped to its geometry snapshot.</summary>
    public int Floor
    {
        get;
    }

    /// <summary>World-space square center.</summary>
    public Point Center
    {
        get;
    }

    /// <summary>World-space width including conservative empty padding.</summary>
    public float Span
    {
        get;
    }

    /// <summary>Reports whether a completed raster already has this exact projection.</summary>
    public bool Matches(FloorPlanRaster raster) => raster != null && raster.Floor == Floor
        && raster.Center.x == Center.x && raster.Center.z == Center.z && raster.Span == Span;

    /// <summary>
    /// Projects this crop into another raster. Outside coordinates sample its opaque fog border,
    /// allowing conservative framing without expanding to undiscovered geometry bounds.
    /// </summary>
    public (float X, float Y, float Size) Crop(FloorPlanRaster raster)
    {
        var size = Span / raster.Span;
        return ((Center.x - raster.Center.x) / raster.Span + 0.5f - size / 2,
            (Center.z - raster.Center.z) / raster.Span + 0.5f - size / 2, size);
    }
}
