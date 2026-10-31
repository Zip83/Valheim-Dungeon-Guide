using System;
using System.IO;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Reference-counts the optional shader bundle without modifying game-owned rendering resources.</summary>
internal static class MapShaderResources
{
    private static AssetBundle s_bundle;
    private static Shader s_shader;
    private static int s_users;
    private static bool s_attempted;
    private static bool s_warned;

    /// <summary>Creates a private material, or returns null to select the CPU renderer.</summary>
    public static Material Acquire()
    {
        if (!s_attempted)
        {
            LoadShader();
        }

        if (!s_shader || !s_shader.isSupported)
        {
            if (!s_warned)
            {
                s_warned = true;
                Debug.LogWarning("DungeonGuide: optional UI shader unavailable; using incremental CPU compositing.");
            }

            return null;
        }

        s_users++;
        return new Material(s_shader);
    }

    private static void LoadShader()
    {
        s_attempted = true;
        var path = Path.Combine(Path.GetDirectoryName(typeof(DungeonGuide).Assembly.Location), "dungeonguide-ui");
        try
        {
            if (File.Exists(path))
            {
                s_bundle = AssetBundle.LoadFromFile(path);
                s_shader = s_bundle ? s_bundle.LoadAsset<Shader>("Assets/DungeonGuideMap.shader") : null;
            }
        }
        catch (Exception error)
        {
            Debug.LogWarning("DungeonGuide shader bundle could not be loaded: " + error.Message);
        }
    }

    /// <summary>Releases one owned material and unloads the bundle after its last consumer exits.</summary>
    public static void Release(Material material)
    {
        if (material)
        {
            UnityEngine.Object.Destroy(material);
            s_users--;
        }

        if (s_users == 0 && s_bundle)
        {
            s_bundle.Unload(true);
            s_bundle = null;
            s_shader = null;
            s_attempted = false;
        }
    }
}
