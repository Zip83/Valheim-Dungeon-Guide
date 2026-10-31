using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Composites one cached floor using dirty mask tiles and an optional private GPU material.</summary>
internal sealed class FloorSurfaceRenderer : IDisposable
{
    private readonly FloorPlanRaster _raster;
    private readonly IncrementalFloorFog _fog;
    private readonly FloorFogMask _publishedFog;
    private readonly Texture2D _mask;
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
        _mask = CreateTexture(raster.Size);
        Material = MapShaderResources.Acquire();
        if (Material)
        {
            Material.SetTexture("_FogMask", _mask);
            Material.SetColor("_VisitedColor", MapPalette.VisitedRoom);
            Material.SetColor("_ScannedColor", MapPalette.ScannedFloor);
        }

        _job = Initialize().GetEnumerator();
    }

    /// <summary>Terrain for the GPU path, or incrementally composited color for the CPU path.</summary>
    public Texture2D Texture
    {
        get;
    }

    /// <summary>Owned UI material, or null for the ordinary UI material and CPU path.</summary>
    public Material Material
    {
        get;
    }

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
            if (Material)
            {
                Material.SetFloat("_RevealAll", reveal ? 1 : 0);
            }
        }

        while (budget.TryStartWork())
        {
            if (_job != null)
            {
                if (_job.MoveNext())
                {
                    continue;
                }

                _job.Dispose();
                _job = null;
            }

            if (!_indexed)
            {
                _indexed = true;
                _fog.Observe(exploration);
                ObserveLatency(exploration);
            }

            if (!_fog.TryTake(out var tile))
            {
                break;
            }

            _job = UpdateTile(tile, exploration, visits, reveal).GetEnumerator();
        }

        if (_uploaded)
        {
            (Material ? _mask : Texture).Apply(false);
            _uploaded = false;
            if (Ready && _job == null && _oldestChange != 0 && Time.unscaledTime >= _nextDiagnostic)
            {
                var milliseconds = (System.Diagnostics.Stopwatch.GetTimestamp() - _oldestChange)
                    * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
                Debug.Log("DungeonGuide fog batch published: floor=" + _raster.Floor
                    + ", elapsedMilliseconds=" + milliseconds.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)
                    + ", gpu=" + (Material != null));
                _nextDiagnostic = Time.unscaledTime + 5;
            }

            if (_job == null)
            {
                _oldestChange = 0;
            }
        }
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

        for (var y = 0; y < _raster.Size; y++)
        {
            yield return 0;
            for (var x = 0; x < _raster.Size; x++)
            {
                var i = y * _raster.Size + x;
                _terrain[i] = _raster.Owners[i] == 0 ? MapPalette.TextureBackground
                    : _raster.Boundary[i] ? MapPalette.FloorOutline : MapPalette.ExploredFloor;
                _cloud[i] = MapPalette.Cloud(x, y);
            }
        }

        if (Material)
        {
            Texture.SetPixels32(_terrain);
            Texture.Apply(false);
        }
    }

    private IEnumerable<byte> UpdateTile(int tile, Exploration exploration, Exploration visits, bool reveal)
    {
        foreach (var step in _fog.Update(tile, exploration))
        {
            yield return step;
        }

        var columns = (_raster.Size + IncrementalFloorFog.TileSize - 1) / IncrementalFloorFog.TileSize;
        var x0 = tile % columns * IncrementalFloorFog.TileSize;
        var y0 = tile / columns * IncrementalFloorFog.TileSize;
        var width = Mathf.Min(IncrementalFloorFog.TileSize, _raster.Size - x0);
        var height = Mathf.Min(IncrementalFloorFog.TileSize, _raster.Size - y0);
        var pixels = new Color32[width * height];
        for (var y = 0; y < height; y++)
        {
            yield return 0;
            for (var x = 0; x < width; x++)
            {
                var i = (y0 + y) * _raster.Size + x0 + x;
                var owner = _raster.Owners[i] - 1;
                var visited = owner >= 0 && visits.Has(_raster.Graph.Nodes[owner]);
                var boundary = _raster.Boundary[i];
                if (Material)
                {
                    pixels[y * width + x] = new Color32(_fog.Fog.Opacity[i], visited ? (byte)255 : (byte)0,
                        owner >= 0 && !boundary ? (byte)255 : (byte)0, 255);
                }
                else
                {
                    var color = _terrain[i];
                    if (owner >= 0 && !boundary)
                    {
                        color = visited ? MapPalette.VisitedRoom : reveal ? MapPalette.ScannedFloor : color;
                    }

                    pixels[y * width + x] = reveal ? color : Color32.Lerp(color, _cloud[i], _fog.Fog.Opacity[i] / 255f);
                }
            }
        }

        (Material ? _mask : Texture).SetPixels32(x0, y0, width, height, pixels);

        for (var y = 0; y < height; y++)
        {
            var offset = (y0 + y) * _raster.Size + x0;
            Array.Copy(_fog.Fog.Opacity, offset, _publishedFog.Opacity, offset, width);
        }

        _uploaded = true;
        _completed.Add(tile);
        Ready |= _completed.Count >= columns * columns;
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
        MapShaderResources.Release(Material);
        UnityEngine.Object.Destroy(Texture);
        UnityEngine.Object.Destroy(_mask);
    }
}
