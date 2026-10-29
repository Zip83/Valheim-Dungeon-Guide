using System;
using System.IO;
using BepInEx;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Composes the dungeon guide services and receives the Unity plugin lifecycle callbacks.
/// </summary>
[BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
public sealed class DungeonGuide : BaseUnityPlugin
{
    private GuideGameLoop _loop;
    private MapHooks _hooks;
    private WorldChangeHooks _changes;
    private float _nextError;

    /// <summary>
    /// Composes services, subscribes to game changes, and installs native-map integration during Unity initialization.
    /// </summary>
    private void Awake()
    {
        var settings = new GuideSettings(Config);
        var notice = new UserNotice();
        var view = new MapViewState
        {
            RevealAll = settings.RevealAll.Value,
            AllowBreakables = settings.Breakables.Value,
            AllowJumps = settings.Jumps.Value
        };
        var physics = new GeometryPhysics();
        var locator = new GraphLocator(physics);
        var scanner = new GeometryScanner(
            physics,
            new PassageEvaluator(
                physics,
                new DoorPassageVerifier(),
                new BreakablePassageVerifier(settings.Approved)),
            new JumpEvaluator(physics));
        var repository = new JsonRunRepository(Path.Combine(Paths.ConfigPath, "DungeonGuide"), new SaveMigration());
        var lifecycle = new SessionLifecycle(repository, Logger, notice);
        var runtime = new GuideRuntime(
            this,
            settings,
            new DungeonLocator(),
            new DungeonActivationPolicy(),
            scanner,
            new FloorDetector(new FloorLandingDetector()),
            lifecycle,
            new ExplorationRecorder(physics),
            new TraversalRecorder(locator),
            Logger,
            notice);
        var floorPolicy = new FloorNavigationPolicy();
        var numbering = new FloorNumbering();
        var navigator = new NavigationCoordinator(new RoutePlanner(), locator, floorPolicy);
        var cursor = new RouteCursor();
        var chests = new ChestActions(
            new ChestSelector(new ChestTargetPolicy()),
            new ChestSnapshotReader(),
            new ChestNameDialog(),
            locator,
            lifecycle,
            notice);
        chests.DestinationRequested += navigator.ToChest;
        var commands = new CommandDispatcher(runtime, view, navigator, chests, settings, notice, floorPolicy);
        var input = new InputRouter(settings, commands.Execute);
        var texture = new MapTextureRenderer(new MapExtentFitter());
        var maps = new NativeMapPresenter(
            view,
            texture,
            new MapViewportController(),
            numbering,
            commands.Execute,
            navigator.ToChest);
        _loop = new GuideGameLoop(
            runtime,
            view,
            input,
            chests,
            navigator,
            texture,
            maps,
            new OverlayPresenter(texture, numbering),
            new NavigationHud(notice, new NavigationInstructionFormatter(cursor, numbering)),
            new DynamicRouteValidator(physics, cursor));
        commands.ViewChanged += _loop.RefreshView;
        commands.FitRequested += maps.Center;
        _hooks = new MapHooks(
            () => maps.DungeonVisible,
            () => view.Dungeon = false,
            () => runtime.Session != null && settings.World.Value.IsDown());
        var destruction = new DestructionRecorder(settings, () => runtime.Session?.Area, runtime.DoorChanged);
        _changes = new WorldChangeHooks(runtime.DoorChanged, destruction.Observe, e => Logger.LogError(e));
        Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded. "
            + "M: map; Alt+M: World/Dungeon; F6: optional overlay. Explored-only by default.");
    }

    /// <summary>
    /// Advances the guide scheduler during the Unity frame update.
    /// </summary>
    private void Update() => Guard(() => _loop?.Tick());

    /// <summary>
    /// Updates native map presentation after the game frame logic.
    /// </summary>
    private void LateUpdate() => Guard(() => _loop?.LateTick());

    /// <summary>
    /// Draws the optional overlay and explicitly active navigation instructions.
    /// </summary>
    private void OnGUI() => Guard(() => _loop?.Draw());

    private void Guard(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            if (Time.unscaledTime >= _nextError)
            {
                _nextError = Time.unscaledTime + 5;
                Logger.LogError(e);
            }
        }
    }

    /// <summary>
    /// Flushes and disposes the active guide session before the game exits.
    /// </summary>
    private void OnApplicationQuit() => _loop?.Dispose();

    /// <summary>
    /// Removes game hooks and disposes owned guide resources when the plugin component is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        _changes?.Dispose();
        _changes = null;
        _hooks?.Dispose();
        _hooks = null;
        _loop?.Dispose();
        _loop = null;
    }
}
