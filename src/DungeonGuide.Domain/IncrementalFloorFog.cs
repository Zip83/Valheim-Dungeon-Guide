using System;
using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Indexes affected fog tiles once and updates only tiles touched by exploration additions.</summary>
internal sealed class IncrementalFloorFog
{
    internal const int TileSize = 32;
    private readonly FloorPlanRaster _raster;
    private readonly Dictionary<string, HashSet<int>> _tiles = new();
    private readonly Queue<int> _dirty = new();
    private readonly HashSet<int> _queued = new();
    private readonly int _columns;
    private readonly int _feather;
    private int _cursor;
    private int _generation = -1;

    /// <summary>Allocates the live mask; callers complete initialization before displaying its texture.</summary>
    public IncrementalFloorFog(FloorPlanRaster raster)
    {
        _raster = raster;
        _columns = (raster.Size + TileSize - 1) / TileSize;
        _feather = Math.Max(1, Math.Min(8, (int)Math.Ceiling(0.4f / raster.Span * raster.Size)));
        Fog = new FloorFogMask(raster);
        for (var i = 0; i < Fog.Opacity.Length; i++)
        {
            Fog.Opacity[i] = 255;
        }
    }

    /// <summary>Live opacity used by both textures and marker visibility.</summary>
    public FloorFogMask Fog
    {
        get;
    }

    /// <summary>Builds a reverse index including neighboring tiles within the feather halo.</summary>
    public IEnumerable<byte> Initialize()
    {
        for (var y = 0; y < _raster.Size; y++)
        {
            yield return 0;
            for (var x = 0; x < _raster.Size; x++)
            {
                var owner = _raster.Owners[y * _raster.Size + x] - 1;
                if (owner < 0)
                {
                    continue;
                }

                var key = _raster.Graph.Nodes[owner].Key;
                if (!_tiles.TryGetValue(key, out var tiles))
                {
                    tiles = new HashSet<int>();
                    _tiles.Add(key, tiles);
                }

                for (var ty = Math.Max(0, y - _feather) / TileSize;
                    ty <= Math.Min(_raster.Size - 1, y + _feather) / TileSize; ty++)
                {
                    for (var tx = Math.Max(0, x - _feather) / TileSize;
                        tx <= Math.Min(_raster.Size - 1, x + _feather) / TileSize; tx++)
                    {
                        tiles.Add(ty * _columns + tx);
                    }
                }
            }
        }
    }

    /// <summary>Queues additions independently for this consumer, rebuilding all tiles only after a load.</summary>
    public void Observe(Exploration exploration)
    {
        if (_generation != exploration.Generation)
        {
            _generation = exploration.Generation;
            _cursor = 0;
            InvalidateAll();
        }

        while (_cursor < exploration.Changes.Count)
        {
            var change = exploration.Changes[_cursor++];
            if (_tiles.TryGetValue(change.Key, out var tiles))
            {
                foreach (var tile in tiles)
                {
                    Queue(tile);
                }
            }
        }
    }

    /// <summary>Invalidates compositing after a mode or room-visit change without rebuilding the raster.</summary>
    public void InvalidateAll()
    {
        for (var tile = 0; tile < _columns * _columns; tile++)
        {
            Queue(tile);
        }
    }

    /// <summary>Returns one pending tile; additions during an in-flight update may queue it again.</summary>
    public bool TryTake(out int tile)
    {
        if (_dirty.Count == 0)
        {
            tile = -1;
            return false;
        }

        tile = _dirty.Dequeue();
        _queued.Remove(tile);
        return true;
    }

    /// <summary>Updates a tile with a halo so feathering is identical across tile boundaries.</summary>
    public IEnumerable<byte> Update(int tile, Exploration exploration)
    {
        var x0 = tile % _columns * TileSize;
        var y0 = tile / _columns * TileSize;
        var width = Math.Min(TileSize, _raster.Size - x0);
        var height = Math.Min(TileSize, _raster.Size - y0);
        var stride = width + _feather * 2;
        var rows = height + _feather * 2;
        var distance = new byte[stride * rows];
        for (var y = 0; y < rows; y++)
        {
            yield return 0;
            for (var x = 0; x < stride; x++)
            {
                var wx = x0 + x - _feather;
                var wy = y0 + y - _feather;
                if (wx < 0 || wy < 0 || wx >= _raster.Size || wy >= _raster.Size)
                {
                    continue;
                }

                var owner = _raster.Owners[wy * _raster.Size + wx] - 1;
                if (owner >= 0 && exploration.Has(_raster.Graph.Nodes[owner]))
                {
                    var left = x > 0 ? distance[y * stride + x - 1] : 0;
                    var down = y > 0 ? distance[(y - 1) * stride + x] : 0;
                    distance[y * stride + x] = (byte)Math.Min(_feather, Math.Min(left, down) + 1);
                }
            }
        }

        for (var y = rows - 1; y >= 0; y--)
        {
            yield return 0;
            for (var x = stride - 1; x >= 0; x--)
            {
                var i = y * stride + x;
                if (distance[i] > 0)
                {
                    var right = x + 1 < stride ? distance[i + 1] : 0;
                    var up = y + 1 < rows ? distance[i + stride] : 0;
                    distance[i] = (byte)Math.Min(distance[i], Math.Min(right, up) + 1);
                }

                if (x >= _feather && x < _feather + width && y >= _feather && y < _feather + height)
                {
                    Fog.Opacity[(y0 + y - _feather) * _raster.Size + x0 + x - _feather] =
                        (byte)(255 - distance[i] * 255 / _feather);
                }
            }
        }
    }

    /// <summary>Queues only tiles whose coverage depends on the changed sample.</summary>
    public void Invalidate(string key)
    {
        if (_tiles.TryGetValue(key, out var tiles))
        {
            foreach (var tile in tiles)
            {
                Queue(tile);
            }
        }
    }

    private void Queue(int tile)
    {
        if (_queued.Add(tile))
        {
            _dirty.Enqueue(tile);
        }
    }
}
