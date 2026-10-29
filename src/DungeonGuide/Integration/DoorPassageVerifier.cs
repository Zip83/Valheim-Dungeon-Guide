using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Recognizes door blockers and checks the game key requirement before routing through them.
/// </summary>
internal sealed class DoorPassageVerifier
{
    private readonly MethodInfo _haveKey = typeof(Door).GetMethod(
        "HaveKey",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    /// <summary>
    /// Recognizes blocker sets consisting of a supported door.
    /// </summary>
    public bool Matches(Collider[] blockers, out Door door)
    {
        door = blockers[0].GetComponentInParent<Door>();
        var selected = door;
        return selected && blockers.All(c => c.GetComponentInParent<Door>() == selected);
    }

    /// <summary>
    /// Asks the game whether the player satisfies a door's required-key condition.
    /// </summary>
    public bool CanOpen(Door door)
    {
        if (!Player.m_localPlayer)
        {
            return false;
        }

        if (door.m_keyItem == null)
        {
            return true;
        }

        return _haveKey != null && (bool)_haveKey.Invoke(door, new object[] { Player.m_localPlayer, false });
    }
}
