using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace DungeonGuide;

/// <summary>
/// Observes native door states and destruction without mutating game behavior.
/// </summary>
internal sealed class WorldChangeHooks : IDisposable
{
    private readonly Harmony _harmony = new("zip.dungeonguide.geometry");
    private static Action<Door> s_doorChanged;
    private static Action<Destructible> s_destroyed;
    private static Action<Exception> s_report;
    private static ConditionalWeakTable<Door, StrongBox<int>> s_doorStates = new();

    /// <summary>
    /// Installs door and destruction callbacks and forwards patch errors to the supplied logger.
    /// </summary>
    public WorldChangeHooks(Action<Door> door, Action<Destructible> destruction, Action<Exception> error)
    {
        s_doorChanged = door;
        s_destroyed = destruction;
        s_report = error;
        InstallDoorHook();

        InstallDestructionHook();
    }

    [SuppressMessage("Style", "IDE1006", Justification = "Harmony requires __instance and __0 for the original door and state.")]
    private static void DoorStateChanged(Door __instance, int __0)
    {
        try
        {
            if (!s_doorStates.TryGetValue(__instance, out var previous))
            {
                s_doorStates.Add(__instance, new StrongBox<int>(__0));
                return;
            }

            if (previous.Value != __0)
            {
                previous.Value = __0;
                s_doorChanged?.Invoke(__instance);
            }
        }
        catch (Exception e)
        {
            s_report?.Invoke(e);
        }
    }

    [SuppressMessage("Style", "IDE1006", Justification = "Harmony requires __instance to inject the destructible being removed.")]
    private static void Destruction(Destructible __instance)
    {
        try
        {
            if (__instance)
            {
                s_destroyed?.Invoke(__instance);
            }
        }
        catch (Exception e)
        {
            s_report?.Invoke(e);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _harmony.UnpatchSelf();
        s_doorChanged = null;
        s_destroyed = null;
        s_report = null;
        s_doorStates = new ConditionalWeakTable<Door, StrongBox<int>>();
    }

    private void InstallDoorHook()
    {
        var state = AccessTools.Method(typeof(Door), "SetState", new[] { typeof(int) });
        if (state != null)
        {
            _harmony.Patch(
                state,
                postfix: new HarmonyMethod(typeof(WorldChangeHooks), nameof(DoorStateChanged)));
        }
        else
        {
            s_report?.Invoke(new MissingMethodException(typeof(Door).FullName, "SetState(int)"));
        }
    }

    private void InstallDestructionHook()
    {
        var destroy = AccessTools.Method(typeof(Destructible), "Destroy", new[] { typeof(HitData) });
        if (destroy != null)
        {
            _harmony.Patch(destroy, prefix: new HarmonyMethod(typeof(WorldChangeHooks), nameof(Destruction)));
        }
        else
        {
            s_report?.Invoke(new MissingMethodException(typeof(Destructible).FullName, "Destroy(HitData)"));
        }
    }
}
