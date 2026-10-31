using System;
using System.Linq;
using BepInEx.Configuration;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Owns BepInEx configuration bindings and persistence of user display and route preferences.
/// </summary>
internal sealed class GuideSettings
{
    private readonly ConfigFile _file;

    /// <summary>
    /// Sampling-grid spacing in world metres.
    /// </summary>
    public readonly ConfigEntry<float> Spacing;

    /// <summary>Horizontal fog discovery radius in metres, still blocked by solid geometry.</summary>
    public readonly ConfigEntry<float> ExploreRadius;

    /// <summary>
    /// Horizontal radius used by manual scans.
    /// </summary>
    public readonly ConfigEntry<float> Radius;

    /// <summary>
    /// Vertical range above and below a manual scan origin.
    /// </summary>
    public readonly ConfigEntry<float> Height;

    /// <summary>
    /// Maximum sampled horizontal columns before reporting an incomplete scan.
    /// </summary>
    public readonly ConfigEntry<int> Columns;

    /// <summary>
    /// Maximum scan work steps per frame, also constrained by elapsed scan time.
    /// </summary>
    public readonly ConfigEntry<int> Budget;

    /// <summary>
    /// Soft main-thread scan budget in milliseconds; a single Unity query cannot be interrupted.
    /// </summary>
    public readonly ConfigEntry<float> MillisecondsPerFrame;

    /// <summary>
    /// Whether explicit manual scans may replace the world map outside dungeon interiors.
    /// </summary>
    public readonly ConfigEntry<bool> AllowOutdoorMapping;

    /// <summary>
    /// Whether all scanned geometry is shown instead of explored floor samples only.
    /// </summary>
    public readonly ConfigEntry<bool> RevealAll;

    /// <summary>
    /// Whether approved breakable passages may be used in routes.
    /// </summary>
    public readonly ConfigEntry<bool> Breakables;

    /// <summary>
    /// Comma-separated exact prefab identities approved as breakable passages.
    /// </summary>
    public readonly ConfigEntry<string> ApprovedPrefabs;

    /// <summary>
    /// Shortcut for the manually enabled standalone map.
    /// </summary>
    public readonly ConfigEntry<KeyboardShortcut> Overlay;

    /// <summary>
    /// Shortcut requesting a geometry rescan.
    /// </summary>
    public readonly ConfigEntry<KeyboardShortcut> Rescan;

    /// <summary>
    /// Shortcut marking the actual selected chest for collection.
    /// </summary>
    public readonly ConfigEntry<KeyboardShortcut> Mark;

    /// <summary>
    /// Shortcut opening the selected chest's name or note dialog.
    /// </summary>
    public readonly ConfigEntry<KeyboardShortcut> Name;

    /// <summary>
    /// Shortcut confirming collection at the actual selected chest.
    /// </summary>
    public readonly ConfigEntry<KeyboardShortcut> Collected;

    /// <summary>
    /// Shortcut cycling known chest navigation targets.
    /// </summary>
    public readonly ConfigEntry<KeyboardShortcut> NextChest;

    /// <summary>
    /// Shortcut requesting an unexplored-floor destination.
    /// </summary>
    public readonly ConfigEntry<KeyboardShortcut> Explore;

    /// <summary>
    /// Shortcut requesting the entrance or explicitly labeled scan-origin fallback.
    /// </summary>
    public readonly ConfigEntry<KeyboardShortcut> Return;

    /// <summary>
    /// Shortcut stopping the current navigation request.
    /// </summary>
    public readonly ConfigEntry<KeyboardShortcut> Stop;

    /// <summary>
    /// Shortcut switching between native world and dungeon maps.
    /// </summary>
    public readonly ConfigEntry<KeyboardShortcut> World;

    /// <summary>Shortcut saving the player's current physical location.</summary>
    public readonly ConfigEntry<KeyboardShortcut> MarkLocation;

    /// <summary>Shortcut cycling saved personal locations.</summary>
    public readonly ConfigEntry<KeyboardShortcut> NextLocation;

    /// <summary>Shortcut renaming the selected personal location.</summary>
    public readonly ConfigEntry<KeyboardShortcut> RenameLocation;

    /// <summary>Shortcut deleting the selected personal location.</summary>
    public readonly ConfigEntry<KeyboardShortcut> RemoveLocation;

    /// <summary>
    /// Binds user-configurable scan budgets, display defaults, traversal options, and shortcuts.
    /// </summary>
    public GuideSettings(ConfigFile file)
    {
        _file = file;
        ExploreRadius = file.Bind("Exploration", "Radius", 25f,
            "Horizontal discovery radius in metres, clamped to 1-100. Walls and physical floors still limit discovery.");
        Spacing = file.Bind("Scan", "GridSpacing", 1.25f, "Sampling grid, metres.");
        Radius = file.Bind("Scan", "ManualRadius", 45f, "Manual scan radius.");
        Height = file.Bind("Scan", "ManualVerticalRange", 120f, "Metres above and below manual scan origin.");
        Columns = file.Bind("Scan", "MaxColumns", 40000, "Maximum columns; reaching the cap is logged as incomplete.");
        Budget = file.Bind(
            "Scan",
            "MaxWorkStepsPerFrame",
            4096,
            "Safety ceiling for scan operations per frame, clamped to 1-8192. MillisecondsPerFrame limits elapsed work time.");
        MillisecondsPerFrame = file.Bind(
            "Scan",
            "MillisecondsPerFrame",
            1f,
            "Soft main-thread budget, clamped to 0.25-4 ms. Individual Unity queries cannot be interrupted.");
        AllowOutdoorMapping = file.Bind(
            "Display",
            "AllowOutdoorMapping",
            false,
            "Allow manual Rescan outside dungeon interiors, including player bases. No automatic outdoor scans.");
        RevealAll = file.Bind(
            "Display",
            "RevealAll",
            false,
            "False: explored floor only. True: all scanned floor. Applies to all map views.");
        Breakables = file.Bind(
            "Routes",
            "AllowBreakables",
            false,
            "Allow only approved, geometrically checked breakable passages.");
        ApprovedPrefabs = file.Bind(
            "Routes",
            "ApprovedBreakablePrefabs",
            "",
            "Comma separated EXACT prefab names verified as breakable passages. Unapproved destructibles remain candidates.");
        Overlay = Key("ToggleMap", KeyCode.F6, "Optional overlay; always hidden on game startup.");
        Rescan = Key(
            "Rescan",
            KeyCode.Insert,
            "Rebuild dungeon geometry; outdoor scans require Display.AllowOutdoorMapping.");
        Mark = Key("MarkChest", KeyCode.F7, "Mark the actual open or aimed chest.");
        Name = Key("NameChest", KeyCode.F7, "Name actual chest.", KeyCode.LeftShift);
        Collected = Key(
            "Collected",
            KeyCode.F7,
            "Confirm collection at actual chest; no F8 selection required.",
            KeyCode.LeftControl);
        MarkLocation = Key("MarkLocation", KeyCode.F7, "Save current location.", KeyCode.LeftAlt);
        NextLocation = Key("NextLocation", KeyCode.F8, "Cycle personal locations.", KeyCode.LeftAlt);
        RenameLocation = Key("RenameLocation", KeyCode.F7, "Rename selected location.", KeyCode.LeftAlt, KeyCode.LeftShift);
        RemoveLocation = Key("RemoveLocation", KeyCode.F7, "Delete selected location.", KeyCode.LeftAlt, KeyCode.LeftControl);
        NextChest = Key("NextChest", KeyCode.F8, "Cycle known chest destinations.");
        Explore = Key("Explore", KeyCode.F9, "Explore unvisited reachable floor.");
        Return = Key("Return", KeyCode.F10, "Guide to known entrance, otherwise scan start explicitly labelled.");
        Stop = Key("Stop", KeyCode.F9, "Stop navigation.", KeyCode.LeftShift);
        World = Key("WorldMap", KeyCode.M, "Toggle Dungeon / World without closing map.", KeyCode.LeftAlt);
    }

    private ConfigEntry<KeyboardShortcut> Key(string key, KeyCode code, string help, params KeyCode[] modifiers) =>
        _file.Bind("Keys", key, new KeyboardShortcut(code, modifiers), help);

    /// <summary>
    /// Checks whether the exact prefab identity is in the configured approval list.
    /// </summary>
    public bool Approved(string prefab) =>
        ApprovedPrefabs.Value.Split(',').Any(p => string.Equals(p.Trim(), prefab, StringComparison.Ordinal));

    /// <summary>
    /// Persists a newly observed approved prefab without adding duplicate identities.
    /// </summary>
    public void RememberDestroyedPrefab(string prefab)
    {
        if (Approved(prefab))
        {
            return;
        }

        ApprovedPrefabs.Value = string.Join(
            ",",
            ApprovedPrefabs.Value.Split(',').Where(p => !string.IsNullOrWhiteSpace(p)).Concat(new[] { prefab }));
        _file.Save();
    }

    /// <summary>
    /// Persists shared map reveal and optional traversal preferences.
    /// </summary>
    public void SaveView(MapViewState state)
    {
        RevealAll.Value = state.RevealAll;
        Breakables.Value = state.AllowBreakables;
        _file.Save();
    }
}
