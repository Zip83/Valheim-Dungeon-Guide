using UnityEngine;

namespace DungeonGuide;

/// <summary>Reads usable hook capabilities from the player's actual inventory and projectile prefab.</summary>
internal sealed class GrappleEquipment
{
    /// <summary>Inventory item whose attack creates the grappling projectile.</summary>
    public ItemDrop.ItemData Item
    {
        get;
    }

    /// <summary>Native attachment policy and maximum tether length.</summary>
    public GrapplingPoint Attachment
    {
        get;
    }

    /// <summary>Maximum straight shot distance, bounded by projectile lifetime and tether length.</summary>
    public float Range
    {
        get;
    }

    /// <summary>Native launch speed in metres per second.</summary>
    public float Speed
    {
        get;
    }

    /// <summary>Native downward projectile acceleration in metres per second squared.</summary>
    public float Gravity
    {
        get;
    }

    /// <summary>Native projectile lifetime in seconds; zero means no expiry.</summary>
    public float Lifetime
    {
        get;
    }

    /// <summary>Collision radius used for conservative arc clearance.</summary>
    public float Radius
    {
        get;
    }

    private GrappleEquipment(ItemDrop.ItemData item, GrapplingPoint attachment, Projectile projectile, float range)
    {
        Item = item;
        Attachment = attachment;
        Range = range;
        Speed = item.m_shared.m_attack.m_projectileVel;
        Gravity = projectile.m_gravity;
        Lifetime = projectile.m_ttl;
        Radius = Mathf.Max(0.1f, projectile.m_rayRadius);
    }

    /// <summary>Checks that the selected hook remains carried, usable, and affordable without changing equipment.</summary>
    public bool IsAvailable(Player player)
    {
        return player && !player.IsDead() && !player.IsTeleporting()
            && player.GetInventory().GetAllItems().Contains(Item)
            && (!Item.m_shared.m_useDurability || Item.m_durability > 0)
            && player.GetStamina() >= Item.m_shared.m_attack.m_attackStamina;
    }

    /// <summary>Projects the native hand-shot offsets while facing the selected landing.</summary>
    public Vector3 ShotOrigin(Vector3 ground, Vector3 landing)
    {
        var attack = Item.m_shared.m_attack;
        var forward = landing - ground;
        forward.y = 0;
        forward = forward.sqrMagnitude > 0.01f ? forward.normalized : Vector3.forward;
        var right = Vector3.Cross(Vector3.up, forward);
        return ground + Vector3.up * attack.m_attackHeight + forward * attack.m_attackRange
            + right * attack.m_attackOffset;
    }

    /// <summary>Finds a durable, affordable native hook; unsupported projectile trajectories are not guessed.</summary>
    public static GrappleEquipment Find(Player player)
    {
        if (!player || player.IsDead() || player.IsTeleporting())
        {
            return null;
        }

        foreach (var item in player.GetInventory().GetAllItems())
        {
            var hook = Inspect(item, player);
            if (hook != null)
            {
                return hook;
            }
        }

        return null;
    }

    private static GrappleEquipment Inspect(ItemDrop.ItemData item, Player player)
    {
        var attack = item.m_shared.m_attack;
        var projectile = attack?.m_attackProjectile?.GetComponent<Projectile>();
        var attachment = projectile?.m_spawnOnHit?.GetComponent<GrapplingPoint>();
        if (!attachment || projectile.m_drag != 0 || attack.m_bowDraw || attack.m_randomVelocity
            || attack.m_cantUseInDungeon || !string.IsNullOrEmpty(attack.m_attackOriginJoint)
            || item.m_shared.m_useDurability && item.m_durability <= 0
            || player.GetStamina() < attack.m_attackStamina)
        {
            return null;
        }

        var lifetimeRange = projectile.m_ttl > 0 ? attack.m_projectileVel * projectile.m_ttl : attachment.m_maxLength;
        var range = Mathf.Min(attachment.m_maxLength, lifetimeRange);
        return range > 0 && !float.IsInfinity(range) && !float.IsNaN(range)
            ? new GrappleEquipment(item, attachment, projectile, range) : null;
    }
}
