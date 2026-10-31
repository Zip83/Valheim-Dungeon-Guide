using System.Collections.Generic;
using UnityEngine;

namespace DungeonGuide;

/// <summary>Predicts native destructible damage from carried equipment without attacking or mutating the world.</summary>
internal sealed class DestructibleDamageProbe
{
    private readonly Dictionary<Destructible, bool> _results = new();
    private int _frame = -1;

    /// <summary>Checks current health, usable attacks, ammunition, native resistances and tool/world-level requirements.</summary>
    public bool CanBreak(Destructible target)
    {
        if (_frame != Time.frameCount)
        {
            _frame = Time.frameCount;
            _results.Clear();
        }

        if (!_results.TryGetValue(target, out var result))
        {
            result = HasHealth(target) && HasEffectiveEquipment(target, Player.m_localPlayer);
            _results[target] = result;
        }

        return result;
    }

    private static bool HasHealth(Destructible target)
    {
        var view = target.GetComponent<ZNetView>();
        return view && view.IsValid() && view.GetZDO().GetFloat(ZDOVars.s_health, target.m_health) > 0;
    }

    private static bool HasEffectiveEquipment(Destructible target, Player player)
    {
        if (!player)
        {
            return false;
        }

        var inventory = player.GetInventory();
        if (CanDamage(target, player.GetCurrentWeapon(), inventory))
        {
            return true;
        }

        foreach (var item in inventory.GetAllItems())
        {
            if (CanDamage(target, item, inventory))
            {
                return true;
            }
        }

        return false;
    }

    private static bool CanDamage(Destructible target, ItemDrop.ItemData item, Inventory inventory)
    {
        if (item == null || item.m_stack <= 0
            || !(item.IsWeapon() || item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Tool)
            || item.m_shared.m_useDurability && item.m_durability <= 0)
        {
            return false;
        }

        return EffectiveAttack(target, item, item.m_shared.m_attack, inventory)
            || EffectiveAttack(target, item, item.m_shared.m_secondaryAttack, inventory);
    }

    private static bool EffectiveAttack(Destructible target, ItemDrop.ItemData item, Attack attack, Inventory inventory)
    {
        if (attack == null || string.IsNullOrEmpty(attack.m_attackAnimation) || attack.m_damageMultiplier <= 0
            || attack.m_cantUseInDungeon || attack.m_attackType == Attack.AttackType.None
            || attack.m_attackType == Attack.AttackType.TriggerProjectile)
        {
            return false;
        }

        if (string.IsNullOrEmpty(item.m_shared.m_ammoType))
        {
            return EffectiveDamage(target, item, attack, null);
        }

        foreach (var ammo in inventory.GetAllItems())
        {
            if (CompatibleAmmo(item, ammo) && EffectiveDamage(target, item, attack, ammo))
            {
                return true;
            }
        }

        return false;
    }

    private static bool CompatibleAmmo(ItemDrop.ItemData weapon, ItemDrop.ItemData ammo)
    {
        var type = ammo.m_shared.m_itemType;
        return ammo.m_stack > 0 && ammo.m_shared.m_ammoType == weapon.m_shared.m_ammoType
            && (type == ItemDrop.ItemData.ItemType.Ammo || type == ItemDrop.ItemData.ItemType.AmmoNonEquipable
                || type == ItemDrop.ItemData.ItemType.Consumable);
    }

    private static bool EffectiveDamage(Destructible target, ItemDrop.ItemData item, Attack attack, ItemDrop.ItemData ammo)
    {
        var hit = new HitData
        {
            m_toolTier = (short)item.m_shared.m_toolTier,
            m_itemWorldLevel = (byte)item.m_worldLevel,
            m_damage = item.GetDamage()
        };
        if (!hit.CheckToolTier(target.m_minToolTier))
        {
            return false;
        }

        if (ammo != null)
        {
            hit.m_damage.Add(ammo.GetDamage());
        }

        hit.m_damage.Modify(attack.m_damageMultiplier);
        hit.ApplyResistance(target.m_damages, out _);
        return hit.GetTotalDamage() > 0;
    }
}
