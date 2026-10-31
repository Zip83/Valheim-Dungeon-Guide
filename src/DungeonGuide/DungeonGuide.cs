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
        var language = new GameLanguage();
        var text = new GuideText(language.Current);
        var settings = new GuideSettings(Config);
        var notice = new UserNotice();

        var view = CreateMapState(settings);
        var physics = new GeometryPhysics();
        var locator = new GraphLocator(physics);
        var lifecycle = CreateSessionLifecycle(notice, text);
        var runtime = CreateRuntime(settings, physics, locator, lifecycle, notice, text);

        var floorPolicy = new FloorNavigationPolicy();
        var numbering = new FloorNumbering(language.Current);
        var navigator = new NavigationCoordinator(new RoutePlanner(), locator, floorPolicy, new GrappleFallbackSearch(physics));
        var cursor = new RouteCursor();
        var chests = CreateChestActions(locator, lifecycle, notice, text);
        chests.DestinationRequested += navigator.ToChest;

        var waypoints = new WaypointActions(runtime, lifecycle, navigator, new NoteDialog(), notice, text);
        var commands = new CommandDispatcher(runtime, view, navigator, chests, waypoints, settings, notice, floorPolicy, text);
        var input = new InputRouter(settings, commands.Execute);

        var texture = new MapTextureRenderer(new MapExtentFitter());
        var maps = new NativeMapPresenter(
            view,
            texture,
            new MapViewportController(),
            numbering,
            commands.Execute,
            new MapTargetSelector(navigator.ToChest, waypoints),
            text);

        _loop = new GuideGameLoop(
            runtime,
            view,
            input,
            chests,
            navigator,
            texture,
            maps,
            new OverlayPresenter(texture, numbering, text),
            new NavigationHud(new NavigationInstructionFormatter(cursor, numbering, text)),
            new DynamicRouteValidator(physics, cursor, text));

        commands.ViewChanged += _loop.RefreshView;
        commands.FitRequested += maps.Center;

        InstallMapHooks(maps, view, runtime, settings);
        InstallWorldChangeHooks(runtime, settings);

        Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.DisplayVersion} loaded. "
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

    private static MapViewState CreateMapState(GuideSettings settings)
    {
        return new MapViewState
        {
            RevealAll = settings.RevealAll.Value,
            AllowBreakables = settings.Breakables.Value
        };
    }

    private static GeometryScanner CreateScanner(GeometryPhysics physics, GuideSettings settings)
    {
        return new GeometryScanner(
            physics,
            new PassageEvaluator(
                physics,
                new DoorPassageVerifier(),
                new BreakablePassageVerifier(settings.Approved)),
            new JumpEvaluator(physics));
    }

    private SessionLifecycle CreateSessionLifecycle(UserNotice notice, GuideText text)
    {
        var repository = new JsonRunRepository(Path.Combine(Paths.ConfigPath, "DungeonGuide"), new SaveMigration());

        return new SessionLifecycle(repository, Logger, notice, text);
    }

    private GuideRuntime CreateRuntime(GuideSettings settings, GeometryPhysics physics, GraphLocator locator,
        SessionLifecycle lifecycle, UserNotice notice, GuideText text)
    {
        return new GuideRuntime(
            this,
            settings,
            new DungeonLocator(text),
            new DungeonActivationPolicy(),
            CreateScanner(physics, settings),
            new FloorDetector(new FloorLandingDetector()),
            lifecycle,
            new ExplorationRecorder(physics, settings),
            new TraversalRecorder(locator, new TraversalProofVerifier()),
            Logger,
            notice);
    }

    private static ChestActions CreateChestActions(GraphLocator locator, SessionLifecycle lifecycle,
        UserNotice notice, GuideText text)
    {
        return new ChestActions(
            new ChestSelector(new ChestTargetPolicy()),
            new ChestSnapshotReader(),
            new NoteDialog(),
            locator,
            lifecycle,
            notice,
            text);
    }

    private void InstallMapHooks(NativeMapPresenter maps, MapViewState view, GuideRuntime runtime, GuideSettings settings)
    {
        _hooks = new MapHooks(
            () => maps.DungeonVisible,
            () => view.Dungeon = false,
            () => runtime.Session != null && settings.World.Value.IsDown());
    }

    private void InstallWorldChangeHooks(GuideRuntime runtime, GuideSettings settings)
    {
        var destruction = new DestructionRecorder(settings, () => runtime.Session?.Area, runtime.DoorChanged);
        _changes = new WorldChangeHooks(runtime.DoorChanged, destruction.Observe, e => Logger.LogError(e));
    }
}
