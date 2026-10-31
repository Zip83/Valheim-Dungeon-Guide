using System;
using UnityEngine;

/// <summary>Owns disposable textures, mesh and render target for isolated map shader pixel checks.</summary>
internal sealed class MapShaderSmokeFixture : IDisposable
{
    private readonly Material _material;
    private readonly Texture2D _terrain;
    private readonly Texture2D _mask;
    private readonly RenderTexture _target;
    private readonly Mesh _mesh;

    /// <summary>Creates an isolated UI drawing surface using the loaded bundle shader.</summary>
    public MapShaderSmokeFixture(Shader shader)
    {
        _material = new Material(shader);
        _terrain = Solid(new Color32(159, 139, 99, 255), false);
        _mask = Solid(new Color32(0, 0, 255, 255), true);
        _target = new RenderTexture(4, 4, 24, RenderTextureFormat.ARGB32);
        _mesh = Quad();
        _material.mainTexture = _terrain;
        _material.SetTexture("_FogMask", _mask);
        _material.SetVector("_VisitedColor", (Color)new Color32(124, 177, 117, 255));
        _material.SetVector("_ScannedColor", (Color)new Color32(91, 109, 121, 255));
        _material.SetFloat("_LinearColorSpace", QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0);
    }

    /// <summary>Renders one independent mode/mask combination and returns an interior pixel.</summary>
    public Color32 Render(Color32 mask, bool reveal, bool clipped = false)
    {
        Fill(_mask, mask);
        _material.SetFloat("_RevealAll", reveal ? 1 : 0);
        if (clipped)
        {
            _material.EnableKeyword("UNITY_UI_CLIP_RECT");
            _material.SetVector("_ClipRect", new Vector4(2, 2, 3, 3));
        }
        else
        {
            _material.DisableKeyword("UNITY_UI_CLIP_RECT");
        }

        return ReadRenderedPixel();
    }

    private Color32 ReadRenderedPixel()
    {
        var previous = RenderTexture.active;
        var read = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        try
        {
            RenderTexture.active = _target;
            DrawQuad();
            read.ReadPixels(new Rect(0, 0, 4, 4), 0, 0);
            read.Apply(false);
            return read.GetPixels32()[10];
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(read);
        }
    }

    private void DrawQuad()
    {
        GL.Clear(true, true, Color.clear);
        GL.PushMatrix();
        try
        {
            GL.LoadOrtho();
            _material.SetPass(0);
            Graphics.DrawMeshNow(_mesh, Matrix4x4.identity);
        }
        finally
        {
            GL.PopMatrix();
        }
    }

    private static Mesh Quad() => new Mesh
    {
        vertices = new[] { new Vector3(0, 0), new Vector3(1, 0), new Vector3(1, 1), new Vector3(0, 1) },
        uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up },
        colors = new[] { Color.white, Color.white, Color.white, Color.white },
        triangles = new[] { 0, 1, 2, 0, 2, 3 }
    };

    private static Texture2D Solid(Color32 color, bool linear)
    {
        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false, linear)
        {
            filterMode = FilterMode.Point
        };
        Fill(texture, color);
        return texture;
    }

    private static void Fill(Texture2D texture, Color32 color)
    {
        var pixels = new Color32[16];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        texture.SetPixels32(pixels);
        texture.Apply(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        UnityEngine.Object.DestroyImmediate(_material);
        UnityEngine.Object.DestroyImmediate(_terrain);
        UnityEngine.Object.DestroyImmediate(_mask);
        UnityEngine.Object.DestroyImmediate(_mesh);
        _target.Release();
        UnityEngine.Object.DestroyImmediate(_target);
    }
}
