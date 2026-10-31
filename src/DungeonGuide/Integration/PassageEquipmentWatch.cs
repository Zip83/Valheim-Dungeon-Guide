using System.Collections.Generic;

namespace DungeonGuide;

/// <summary>Detects changes that require fresh key, ammunition or destructible-tool permissions.</summary>
internal sealed class PassageEquipmentWatch
{
    private readonly Dictionary<ItemDrop.ItemData, (int Quality, int World, int Tier, bool Usable)> _known = new();
    private Player _player;
    private ItemDrop.ItemData _weapon;
    private int _worldLevel;
    private bool _lockedTools;

    /// <summary>Compares item identities and capability boundaries, ignoring ordinary positive-stack and durability changes.</summary>
    public bool Changed(Player player)
    {
        var items = player.GetInventory().GetAllItems();
        var weapon = player.GetCurrentWeapon();
        var locked = ZoneSystem.instance && ZoneSystem.instance.GetGlobalKey(GlobalKeys.WorldLevelLockedTools);
        if (_player == player && _weapon == weapon && _worldLevel == Game.m_worldLevel && _lockedTools == locked
            && Matches(items))
        {
            return false;
        }

        _player = player;
        _weapon = weapon;
        _worldLevel = Game.m_worldLevel;
        _lockedTools = locked;
        _known.Clear();
        foreach (var item in items)
        {
            _known[item] = State(item);
        }

        return true;
    }

    private bool Matches(List<ItemDrop.ItemData> items)
    {
        if (_known.Count != items.Count)
        {
            return false;
        }

        foreach (var item in items)
        {
            if (!_known.TryGetValue(item, out var known) || known != State(item))
            {
                return false;
            }
        }

        return true;
    }

    private static (int Quality, int World, int Tier, bool Usable) State(ItemDrop.ItemData item) =>
        (item.m_quality, item.m_worldLevel, item.m_shared.m_toolTier,
            item.m_stack > 0 && (!item.m_shared.m_useDurability || item.m_durability > 0));

    /// <summary>Releases item references when the active dungeon session closes.</summary>
    public void Reset()
    {
        _known.Clear();
        _player = null;
        _weapon = null;
    }
}
