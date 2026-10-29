namespace DungeonGuide;

/// <summary>
/// Defines the plugin identity and version reported to the loader.
/// </summary>
internal static class PluginInfo
{
    /// <summary>
    /// Identifies this plugin across builds and installations in BepInEx.
    /// </summary>
    public const string Guid = "zip.dungeonguide";

    /// <summary>
    /// Specifies the display name used in plugin metadata and startup logging.
    /// </summary>
    public const string Name = "Valheim Dungeon Guide";

    /// <summary>
    /// Specifies the plugin version reported to BepInEx.
    /// </summary>
    public const string Version = "0.1.0";
}
