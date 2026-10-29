using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;

namespace DungeonGuide;

/// <summary>
/// Observes door interaction and destruction to invalidate affected navigation geometry.
/// </summary>
internal sealed class WorldChangeHooks : IDisposable
{
    private readonly Harmony _harmony = new("zip.dungeonguide.geometry");
    private static Action s_doorChanged;
    private static Action<Destructible> s_destroyed;
    private static Action<Exception> s_report;

    /// <summary>
    /// Installs door and destruction callbacks and forwards patch errors to the supplied logger.
    /// </summary>
    public WorldChangeHooks(Action door, Action<Destructible> destruction, Action<Exception> error)
    {
        s_doorChanged = door;
        s_destroyed = destruction;
        s_report = error;
        var interact = AccessTools.Method(typeof(Door), "Interact");
        if (interact != null)
        {
            _harmony.Patch(
                interact,
                postfix: new HarmonyMethod(typeof(WorldChangeHooks), nameof(DoorInteraction)));
        }

        var destroy = AccessTools.Method(typeof(Destructible), "Destroy", new[] { typeof(HitData) });
        if (destroy != null)
        {
            _harmony.Patch(destroy, prefix: new HarmonyMethod(typeof(WorldChangeHooks), nameof(Destruction)));
        }
    }

    [SuppressMessage("Style", "IDE1006", Justification = "Harmony binds the original return value through the exact __result parameter name.")]
    private static void DoorInteraction(bool __result)
    {
        if (__result)
        {
            try
            {
                s_doorChanged?.Invoke();
            }
            catch (Exception e)
            {
                s_report?.Invoke(e);
            }
        }
    }

    [SuppressMessage("Style", "IDE1006", Justification = "Harmony requires __instance and __0 to inject the original instance and first argument.")]
    private static void Destruction(Destructible __instance, HitData __0)
    {
        try
        {
            if (__0 != null && Player.m_localPlayer && __0.GetAttacker() == Player.m_localPlayer)
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
    }
}
