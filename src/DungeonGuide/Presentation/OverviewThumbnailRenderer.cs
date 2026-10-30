using System;
using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Owns one cached floor thumbnail with the same conservative coverage and fog as the detail map.</summary>
internal sealed class OverviewThumbnailRenderer : IDisposable
{
    private const int Size = OverviewStyle.TextureSize;

    private readonly FloorPlanCache _plan = new();
    private readonly FloorFogCache _fog = new();
    private readonly Color32[] _pixels = new Color32[Size * Size];

    private IEnumerator<byte> _paint;
    private FloorPlanRaster _painting;
    private FloorFogMask _paintingFog;

    /// <summary>Completed thumbnail image, or null while the first image is being prepared.</summary>
    public Texture2D Texture
    {
        get; private set;
    }

    /// <summary>Advances coverage, fog, and painting using the overview's shared frame budget.</summary>
    public void Tick(DungeonSession session, OverviewFloor floor, bool revealAll, FrameWorkBudget budget)
    {
        var center = new Point((floor.Minimum.x + floor.Maximum.x) / 2, 0,
            (floor.Minimum.z + floor.Maximum.z) / 2);
        var span = Mathf.Max(floor.Maximum.x - floor.Minimum.x, floor.Maximum.z - floor.Minimum.z)
            + session.Scan.Spacing * 3;

        _plan.Tick(session.Scan.Graph, floor.Floor, session.Scan.Spacing, Size, center, span, budget);
        var raster = _plan.Ready;
        if (raster == null)
        {
            return;
        }

        _fog.Tick(raster, session.Exploration, budget);
        var fog = _fog.Ready;
        if (fog == null || fog.Raster != raster)
        {
            return;
        }

        PreparePaint(raster, fog, revealAll);
        AdvancePaint(budget);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _paint?.Dispose();
        _paint = null;

        _plan.Dispose();
        _fog.Dispose();

        if (Texture)
        {
            UnityEngine.Object.Destroy(Texture);
        }

        Texture = null;
    }

    private void PreparePaint(FloorPlanRaster raster, FloorFogMask fog, bool revealAll)
    {
        if (_painting == raster && (_paint != null || _paintingFog == fog))
        {
            return;
        }

        _paint?.Dispose();
        _painting = raster;
        _paintingFog = fog;
        _paint = Paint(raster, fog, revealAll).GetEnumerator();
    }

    private void AdvancePaint(FrameWorkBudget budget)
    {
        while (_paint != null && budget.TryStartWork())
        {
            if (!_paint.MoveNext())
            {
                Publish();
                _paint.Dispose();
                _paint = null;
            }
        }
    }

    private IEnumerable<byte> Paint(FloorPlanRaster raster, FloorFogMask fog, bool revealAll)
    {
        for (var y = 0; y < Size; y++)
        {
            yield return 0;

            for (var x = 0; x < Size; x++)
            {
                var index = y * Size + x;
                var surface = raster.Owners[index] == 0 ? MapPalette.TextureBackground
                    : raster.Boundary[index] ? MapPalette.FloorOutline : MapPalette.ExploredFloor;
                _pixels[index] = revealAll ? surface
                    : Color32.Lerp(surface, MapPalette.Cloud(x, y), fog.Opacity[index] / 255f);
            }
        }
    }

    private void Publish()
    {
        if (!Texture)
        {
            Texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        Texture.SetPixels32(_pixels);
        Texture.Apply(false);
    }
}
