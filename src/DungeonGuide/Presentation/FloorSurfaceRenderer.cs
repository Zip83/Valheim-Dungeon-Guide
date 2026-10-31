using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Composites one cached floor using dirty mask tiles and bounded CPU compositing.</summary>
internal sealed class FloorSurfaceRenderer : IDisposable
{
    private readonly FloorPlanRaster _raster;
    private readonly IncrementalFloorFog _fog;
    private readonly FloorFogMask _publishedFog;
    private readonly Color32[] _terrain;
    private readonly Color32[] _cloud;
    private IEnumerator<byte> _job;
    private bool _indexed;
    private bool _reveal;
    private int _visits = -1;
    private readonly HashSet<int> _completed = new();
    private int _visitCursor;
    private int _changeCursor;
    private long _oldestChange;
    private float _nextDiagnostic;
    private bool _uploaded;

    /// <summary>Allocates owned textures; first publication waits for all initial tiles.</summary>
    public FloorSurfaceRenderer(FloorPlanRaster raster)
    {
        _raster = raster;
        _fog = new IncrementalFloorFog(raster);
        _publishedFog = new FloorFogMask(raster);
        Array.Copy(_fog.Fog.Opacity, _publishedFog.Opacity, _publishedFog.Opacity.Length);
        _terrain = new Color32[raster.Size * raster.Size];
        _cloud = new Color32[_terrain.Length];
        Texture = CreateTexture(raster.Size);

        _job = Initialize().GetEnumerator();
    }

    /// <summary>Terrain for the GPU path, or incrementally composited color for the CPU path.</summary>
    public Texture2D Texture
    {
        get;
    }

    /// <summary>Default UI material for CPU compositing.</summary>
    public Material Material => null;

    /// <summary>Whether all tiles have a valid initial image.</summary>
    public bool Ready
    {
        get; private set;
    }

    /// <summary>Live fog in exactly the same projection as the texture.</summary>
    public FloorFogMask Fog => _publishedFog;

    /// <summary>Advances pending tile work without regenerating static geometry or repainting unrelated pixels.</summary>
    public void Tick(Exploration exploration, Exploration visits, bool reveal, FrameWorkBudget budget)
    {
        if (_indexed)
        {
            ObserveState(exploration, visits, reveal);
        }

        AdvanceWork(exploration, visits, reveal, budget);
        PublishChanges();
    }

    private void ObserveState(Exploration exploration, Exploration visits, bool reveal)
    {
        _fog.Observe(exploration);
        ObserveLatency(exploration);
        if (_reveal != reveal || _visits != visits.Generation)
        {
            _fog.InvalidateAll();
            _visits = visits.Generation;
            _visitCursor = 0;
        }

        while (_visitCursor < visits.Changes.Count)
        {
            _fog.Invalidate(visits.Changes[_visitCursor++].Key);
        }

        _reveal = reveal;
    }

    private void AdvanceWork(Exploration exploration, Exploration visits, bool reveal, FrameWorkBudget budget)
    {
        while (budget.TryStartWork())
        {
            if (_job != null && _job.MoveNext())
            {
                continue;
            }

            _job?.Dispose();
            _job = null;
            ObserveInitialIndex(exploration);
            if (!_fog.TryTake(out var tile))
            {
                break;
            }

            _job = UpdateTile(tile, exploration, visits, reveal).GetEnumerator();
        }
    }

    private void ObserveInitialIndex(Exploration exploration)
    {
        if (!_indexed)
        {
            _indexed = true;
            _fog.Observe(exploration);
            ObserveLatency(exploration);
        }
    }

    private void PublishChanges()
    {
        if (!_uploaded)
        {
            return;
        }

        Texture.Apply(false);
        _uploaded = false;
        if (Ready && _job == null && _oldestChange != 0 && Time.unscaledTime >= _nextDiagnostic)
        {
            LogLatency();
        }

        if (_job == null)
        {
            _oldestChange = 0;
        }
    }

    private void LogLatency()
    {
        var milliseconds = (System.Diagnostics.Stopwatch.GetTimestamp() - _oldestChange)
            * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        Debug.Log("DungeonGuide fog batch published: floor=" + _raster.Floor
            + ", elapsedMilliseconds=" + milliseconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)
            + ", gpu=" + (Material != null));
        _nextDiagnostic = Time.unscaledTime + 5;
    }

    private void ObserveLatency(Exploration exploration)
    {
        while (_changeCursor < exploration.Changes.Count)
        {
            var change = exploration.Changes[_changeCursor++];
            if (Ready && change.Floor == _raster.Floor && _oldestChange == 0)
            {
                _oldestChange = change.Timestamp;
            }
        }
    }

    private IEnumerable<byte> Initialize()
    {
        foreach (var step in _fog.Initialize())
        {
            yield return step;
        }

        foreach (var step in PrepareTerrain())
        {
            yield return step;
        }
    }

    private IEnumerable<byte> PrepareTerrain()
    {
        for (var y = 0; y < _raster.Size; y++)
        {
            yield return 0;
            PaintTerrainRow(y);
        }
    }

    private void PaintTerrainRow(int y)
    {
        for (var x = 0; x < _raster.Size; x++)
        {
            var index = y * _raster.Size + x;
            _terrain[index] = _raster.Owners[index] == 0 ? MapPalette.TextureBackground
                : _raster.Boundary[index] ? MapPalette.FloorOutline : MapPalette.ExploredFloor;
            _cloud[index] = MapPalette.Cloud(x, y);
        }
    }

    private IEnumerable<byte> UpdateTile(int tile, Exploration exploration, Exploration visits, bool reveal)
    {
        foreach (var step in _fog.Update(tile, exploration))
        {
            yield return step;
        }

        var region = new FogTileRegion(_raster.Size, tile, IncrementalFloorFog.TileSize, 0);
        var pixels = new Color32[region.Width * region.Height];
        foreach (var step in PaintTile(region, visits, reveal, pixels))
        {
            yield return step;
        }

        UploadTile(tile, region, pixels);
    }

    private IEnumerable<byte> PaintTile(FogTileRegion region, Exploration visits, bool reveal, Color32[] pixels)
    {
        for (var y = 0; y < region.Height; y++)
        {
            yield return 0;
            for (var x = 0; x < region.Width; x++)
            {
                var index = (region.Y + y) * _raster.Size + region.X + x;
                pixels[y * region.Width + x] = ComposePixel(index, visits, reveal);
            }
        }
    }

    private Color32 ComposePixel(int index, Exploration visits, bool reveal)
    {
        var owner = _raster.Owners[index] - 1;
        var visited = owner >= 0 && visits.Has(_raster.Graph.Nodes[owner]);
        var color = _terrain[index];
        if (owner >= 0 && !_raster.Boundary[index])
        {
            color = visited ? MapPalette.VisitedRoom : reveal ? MapPalette.ScannedFloor : color;
        }

        return reveal ? color : Color32.Lerp(color, _cloud[index], _fog.Fog.Opacity[index] / 255f);
    }

    private void UploadTile(int tile, FogTileRegion region, Color32[] pixels)
    {
        Texture.SetPixels32(region.X, region.Y, region.Width, region.Height, pixels);
        PublishFog(region);
        _uploaded = true;
        _completed.Add(tile);
        Ready |= _completed.Count >= region.TotalTiles;
    }

    private void PublishFog(FogTileRegion region)
    {
        for (var y = 0; y < region.Height; y++)
        {
            var offset = (region.Y + y) * _raster.Size + region.X;
            Array.Copy(_fog.Fog.Opacity, offset, _publishedFog.Opacity, offset, region.Width);
        }
    }

    private static Texture2D CreateTexture(int size) => new(size, size, TextureFormat.RGBA32, false)
    {
        filterMode = FilterMode.Bilinear,
        wrapMode = TextureWrapMode.Clamp
    };

    /// <inheritdoc />
    public void Dispose()
    {
        _job?.Dispose();
        UnityEngine.Object.Destroy(Texture);
    }
}
