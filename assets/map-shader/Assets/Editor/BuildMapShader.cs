using System.IO;
using UnityEditor;
using UnityEngine.Rendering;

/// <summary>Builds the isolated UI bundle for the exact target editor and explicit graphics APIs.</summary>
public static class BuildMapShader
{
    /// <summary>Batch-mode entry point; rejects an editor different from Valheim's Unity baseline.</summary>
    public static void Build()
    {
        if (!UnityEngine.Application.unityVersion.StartsWith("6000.0.75"))
        {
            throw new System.InvalidOperationException("Use Unity 6000.0.75f1 for the Valheim 1.0.21 UI bundle.");
        }

        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
            new[] { GraphicsDeviceType.Direct3D11, GraphicsDeviceType.Vulkan });
        Directory.CreateDirectory("BundleOutput");
        var manifest = BuildPipeline.BuildAssetBundles("BundleOutput", new[]
        {
            new AssetBundleBuild
            {
                assetBundleName = "dungeonguide-ui",
                assetNames = new[] { "Assets/DungeonGuideMap.shader" }
            }
        }, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
            BuildTarget.StandaloneWindows64);
        if (manifest == null)
        {
            throw new System.InvalidOperationException("UI shader bundle build failed.");
        }
    }
}
