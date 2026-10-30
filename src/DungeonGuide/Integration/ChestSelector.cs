using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DungeonGuide;

/// <summary>
/// Resolves the visible open or interaction-hovered container using game range checks.
/// </summary>
internal sealed class ChestSelector
{
    private readonly FieldInfo _currentContainer = typeof(InventoryGui).GetField(
        "m_currentContainer",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private readonly FieldInfo _containerPanel = typeof(InventoryGui).GetField(
        "m_container",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    private readonly ChestTargetPolicy _policy;

    /// <summary>
    /// Uses the pure interaction-selection policy after checking native container visibility and range.
    /// </summary>
    public ChestSelector(ChestTargetPolicy policy)
    {
        _policy = policy;
    }

    /// <summary>
    /// Returns the actual interactive open or aimed container, or null when none satisfies game range checks.
    /// </summary>
    public Container Select(Player player)
    {
        var gui = InventoryGui.instance;
        var open = gui ? _currentContainer?.GetValue(gui) as Container : null;
        var panel = gui ? _containerPanel?.GetValue(gui) as Component : null;
        var hover = player.GetHoverObject();
        var aimed = hover ? hover.GetComponentInParent<Container>() : null;
        var opened = gui && InventoryGui.IsVisible() && panel && panel.gameObject.activeInHierarchy && open;
        switch (_policy.Select(
            opened,
            open && InRange(open, player),
            aimed && !string.IsNullOrEmpty(aimed.GetHoverText()),
            aimed && InRange(aimed, player)))
        {
            case ChestTargetKind.Open:
                return open;
            case ChestTargetKind.Aimed:
                return aimed;
            default:
                return null;
        }
    }

    private bool InRange(Container chest, Player player)
    {
        var colliders = chest.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).ToArray();
        var range = player.m_maxInteractDistance + 0.5f;
        if (colliders.Length == 0)
        {
            return Vector3.Distance(player.transform.position, chest.transform.position) < range;
        }

        return colliders.Any(c => Vector3.Distance(
            c.ClosestPoint(player.transform.position + Vector3.up),
            player.transform.position + Vector3.up)
                < range);
    }
}
