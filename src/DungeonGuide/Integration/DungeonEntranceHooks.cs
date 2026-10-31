using System;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;

namespace DungeonGuide;

/// <summary>Observes successful native dungeon transfers without changing teleport behavior or other patches.</summary>
internal sealed class DungeonEntranceHooks : IDisposable
{
    private readonly Harmony _harmony = new("zip.dungeonguide.entrance");
    private static Action<Teleport, Humanoid> s_observe;
    private static Action<Exception> s_report;

    /// <summary>Installs a read-only postfix and isolates observer failures from the game's interaction.</summary>
    public DungeonEntranceHooks(Action<Teleport, Humanoid> observe, Action<Exception> report)
    {
        s_observe = observe;
        s_report = report;
        var method = AccessTools.Method(typeof(Teleport), nameof(Teleport.Interact),
            new[] { typeof(Humanoid), typeof(bool), typeof(bool) });
        if (method == null)
        {
            throw new MissingMethodException(typeof(Teleport).FullName, nameof(Teleport.Interact));
        }

        _harmony.Patch(method, postfix: new HarmonyMethod(typeof(DungeonEntranceHooks), nameof(Observe)));
    }

    [SuppressMessage("Style", "IDE1006", Justification = "Harmony requires original instance, argument and result names.")]
    private static void Observe(Teleport __instance, Humanoid __0, bool __result)
    {
        if (!__result)
        {
            return;
        }

        try
        {
            s_observe?.Invoke(__instance, __0);
        }
        catch (Exception error)
        {
            s_report?.Invoke(error);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _harmony.UnpatchSelf();
        s_observe = null;
        s_report = null;
    }
}
