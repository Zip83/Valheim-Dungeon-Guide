using System;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Owns the ready Full and FOG CPU images for immediate visibility switching.</summary>
internal sealed class FloorSurfaceImages : IDisposable
{
    private readonly Texture2D _fog;
    private readonly Texture2D _full;
    private bool _reveal;

    /// <summary>Allocates both color images; the surface publishes only after their first tiles finish.</summary>
    public FloorSurfaceImages(int size)
    {
        _fog = CreateTexture(size);
        _full = CreateTexture(size);
    }

    /// <summary>Ready image selected without rebuilding or uploading pixels.</summary>
    public Texture2D Texture => _reveal ? _full : _fog;

    /// <summary>Changes only which maintained CPU image is displayed.</summary>
    public void SetMode(bool reveal) => _reveal = reveal;

    /// <summary>Updates both color variants from the same completed tile.</summary>
    public void UploadTile(FogTileRegion region, Color32[] fog, Color32[] full)
    {
        _fog.SetPixels32(region.X, region.Y, region.Width, region.Height, fog);
        _full.SetPixels32(region.X, region.Y, region.Width, region.Height, full);
    }

    /// <summary>Publishes completed changes together so either mode is immediately available.</summary>
    public void ApplyChanges()
    {
        _fog.Apply(false);
        _full.Apply(false);
    }

    private static Texture2D CreateTexture(int size) => new(size, size, TextureFormat.RGBA32, false)
    {
        filterMode = FilterMode.Bilinear,
        wrapMode = TextureWrapMode.Clamp
    };

    /// <summary>Releases both owned images when their floor is retired.</summary>
    public void Dispose()
    {
        UnityEngine.Object.Destroy(_fog);
        UnityEngine.Object.Destroy(_full);
    }
}
