using UnityEngine;

/// <summary>
/// EX skill: lays a carpet of fire from behind the tank to in front of it, along the aim direction.
/// Zones inherit a share of the basic attack's burn, like the slam does.
/// </summary>
// WHY: this is the skill that motivated charges. Its first upgrade adds re-uses rather than a
// shorter cooldown, which a boolean "on cooldown / not" cannot express — the slot tracks a charge
// count instead, and a one-charge skill is just the MaxCharges = 1 case.
public class FireCarpetBehaviour : SkillBehaviour
{
    public const string P_ZoneCount = "zoneCount";
    public const string P_LengthAhead = "lengthAhead";
    public const string P_LengthBehind = "lengthBehind";
    public const string P_ZoneDamage = "zoneDamagePerTick";
    public const string P_ZoneRadius = "zoneRadius";
    public const string P_ZoneInherit = "zoneInherit";
    public const string P_ZoneTickInterval = "zoneTickInterval";

    public override void OnActivate(SkillContext ctx)
    {
        int count = Mathf.Max(2, ctx.Values.GetInt(P_ZoneCount, 6));
        float ahead = ctx.Values.Get(P_LengthAhead, 6f);
        float behind = ctx.Values.Get(P_LengthBehind, 2f);

        Vector2 direction = ctx.AimDirection;
        Vector2 start = (Vector2)ctx.Owner.position - direction * behind;
        Vector2 end = (Vector2)ctx.Owner.position + direction * ahead;

        GroundZoneSource basicZone = FindBasicZoneSource(ctx);

        int damage = ctx.Values.Damage(P_ZoneDamage);
        float radius = ctx.Values.Area(P_ZoneRadius);
        float inherit = ctx.Values.Get(P_ZoneInherit, 0.4f);
        float tick = ctx.Values.Get(P_ZoneTickInterval, 0f);

        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            Vector2 position = Vector2.Lerp(start, end, t);

            GroundZoneSource.Spawn(ctx.Pool, basicZone, position, damage, radius, inherit, tick, this);
        }
    }

    private GroundZoneSource FindBasicZoneSource(SkillContext ctx)
    {
        TankSkillLoadout loadout = ctx.Owner.GetComponentInChildren<TankSkillLoadout>();
        if (loadout == null) loadout = ctx.Owner.GetComponentInParent<TankSkillLoadout>();
        if (loadout == null) return null;

        FireballBasicBehaviour basic = loadout.GetComponentInChildren<FireballBasicBehaviour>(true);
        return basic != null ? basic.ZoneSource : null;
    }
}
