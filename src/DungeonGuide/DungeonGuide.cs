using BepInEx;

namespace DungeonGuide;

/// <summary>
/// Provides the BepInEx entry point for Valheim Dungeon Guide.
/// </summary>
[BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
public sealed class DungeonGuide : BaseUnityPlugin
{
    /// <summary>
    /// Reports successful plugin startup when Unity initializes this component.
    /// </summary>
    /// <remarks>
    /// Unity invokes this message callback as part of the component lifecycle.
    /// </remarks>
    private void Awake()
    {
        Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded.");
    }
}
