using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Checks the compiled map bundle on the active graphics API without modifying Valheim.</summary>
public static class MapShaderSmokeCheck
{
    /// <summary>Builds the bundle and checks five independent UI rendering behaviors.</summary>
    public static void Run()
    {
        BuildMapShader.Build();
        var bundle = AssetBundle.LoadFromFile("BundleOutput/dungeonguide-ui");
        if (!bundle)
        {
            throw new InvalidOperationException("Compiled map bundle failed to load.");
        }

        try
        {
            RunChecks(RequireShader(bundle));
            WriteReport();
        }
        finally
        {
            bundle.Unload(true);
        }
    }

    /// <summary>Selects linear rendering for the next Editor invocation.</summary>
    public static void UseLinearColorSpace()
    {
        PlayerSettings.colorSpace = ColorSpace.Linear;
        AssetDatabase.SaveAssets();
    }

    /// <summary>Selects gamma rendering for the next Editor invocation.</summary>
    public static void UseGammaColorSpace()
    {
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        AssetDatabase.SaveAssets();
    }

    private static Shader RequireShader(AssetBundle bundle)
    {
        var shader = bundle.LoadAsset<Shader>("Assets/DungeonGuideMap.shader");
        if (!shader || !shader.isSupported || ShaderUtil.ShaderHasError(shader))
        {
            throw new InvalidOperationException("Map shader is missing, unsupported or has compilation errors.");
        }

        return shader;
    }

    private static void RunChecks(Shader shader)
    {
        using (var fixture = new MapShaderSmokeFixture(shader))
        {
            CheckExplored(fixture);
            CheckFullMap(fixture);
            CheckVisited(fixture);
            CheckFog(fixture);
            CheckClipping(fixture);
        }
    }

    private static void CheckExplored(MapShaderSmokeFixture fixture) =>
        Expect(fixture.Render(new Color32(0, 0, 255, 255), false), new Color32(159, 139, 99, 255), "explored floor");

    private static void CheckFullMap(MapShaderSmokeFixture fixture) =>
        Expect(fixture.Render(new Color32(0, 0, 255, 255), true), new Color32(91, 109, 121, 255), "unvisited full map");

    private static void CheckVisited(MapShaderSmokeFixture fixture) =>
        Expect(fixture.Render(new Color32(0, 255, 255, 255), true), new Color32(124, 177, 117, 255), "entered room");

    private static void CheckFog(MapShaderSmokeFixture fixture)
    {
        var fog = fixture.Render(new Color32(255, 0, 255, 255), false);
        if (fog.r > 110 || fog.g > 120 || fog.b > 130 || fog.a < 250)
        {
            throw new InvalidOperationException("Opaque fog did not hide floor geometry: " + fog);
        }
    }

    private static void CheckClipping(MapShaderSmokeFixture fixture) =>
        Expect(fixture.Render(new Color32(0, 0, 255, 255), true, true), new Color32(0, 0, 0, 0), "UI clipping");

    private static void Expect(Color32 actual, Color32 expected, string label)
    {
        if (Math.Abs(actual.r - expected.r) > 3 || Math.Abs(actual.g - expected.g) > 3
            || Math.Abs(actual.b - expected.b) > 3 || Math.Abs(actual.a - expected.a) > 3)
        {
            throw new InvalidOperationException(label + ": expected " + expected + ", got " + actual);
        }
    }

    private static void WriteReport()
    {
        var report = "{\"unity\":\"" + Application.unityVersion + "\",\"api\":\""
            + SystemInfo.graphicsDeviceType + "\",\"colorSpace\":\"" + QualitySettings.activeColorSpace
            + "\",\"checksPassed\":5}";
        File.WriteAllText("BundleOutput/smoke-" + SystemInfo.graphicsDeviceType + "-"
            + QualitySettings.activeColorSpace + ".json", report);
        Debug.Log("DungeonGuide shader smoke checks passed: " + report);
    }
}
