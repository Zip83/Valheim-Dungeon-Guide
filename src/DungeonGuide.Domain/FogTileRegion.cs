using System;

namespace DungeonGuide;

/// <summary>Defines a clipped tile and the local halo coordinates used by fog distance passes.</summary>
internal readonly struct FogTileRegion
{
    private readonly int _x;
    private readonly int _y;
    private readonly int _width;
    private readonly int _height;
    private readonly int _halo;

    /// <summary>Clips a tile to the raster and reserves the same feather halo on all four sides.</summary>
    public FogTileRegion(int size, int tile, int tileSize, int halo)
    {
        var columns = (size + tileSize - 1) / tileSize;
        _x = tile % columns * tileSize;
        _y = tile / columns * tileSize;
        _width = Math.Min(tileSize, size - _x);
        _height = Math.Min(tileSize, size - _y);
        _halo = halo;
    }

    /// <summary>Width of each working row, including halo pixels.</summary>
    public int Stride => _width + _halo * 2;

    /// <summary>Number of working rows, including halo pixels.</summary>
    public int Rows => _height + _halo * 2;

    /// <summary>Maps a working-column index into the complete raster.</summary>
    public int WorldX(int x) => _x + x - _halo;

    /// <summary>Maps a working-row index into the complete raster.</summary>
    public int WorldY(int y) => _y + y - _halo;

    /// <summary>Excludes halo pixels from publication into the live mask.</summary>
    public bool ContainsInterior(int x, int y) => x >= _halo && x < _halo + _width
        && y >= _halo && y < _halo + _height;
}
