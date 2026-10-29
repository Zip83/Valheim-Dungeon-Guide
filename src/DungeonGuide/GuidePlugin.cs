using BepInEx;

namespace DungeonGuide;

[BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
public sealed class GuidePlugin : BaseUnityPlugin
{
    private void Awake()
    {
        Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded.");
    }
}
