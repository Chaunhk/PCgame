using UnityEngine;

/// <summary>
/// Sub skill: slam the ground for an instant explosion, then leave a burning zone whose numbers are
/// its own base plus a share of the basic attack's zone.
/// </summary>
public class GroundSlamBehaviour : SkillBehaviour
{
    public const string P_BlastDamage = "blastDamage";
    public const string P_BlastRadius = "blastRadius";
    public const string P_ZoneDamage = "zoneDamagePerTick";
    public const string P_ZoneRadius = "zoneRadius";
    public const string P_ZoneInherit = "zoneInherit";
    public const string P_ZoneTickInterval = "zoneTickInterval";

    [SerializeField] private LayerMask _hitMask = ~0;

    public override void OnActivate(SkillContext ctx)
    {
        Vector3 origin = ctx.Owner.position;

        // 1. the instant blast
        int blastDamage = ctx.Values.Damage(P_BlastDamage);
        float blastRadius = ctx.Values.Area(P_BlastRadius);

        Collider2D[] caught = Physics2D.OverlapCircleAll(origin, blastRadius, _hitMask);
        for (int i = 0; i < caught.Length; i++)
        {
            IDamageable target = caught[i].GetComponent<IDamageable>();
            if (target == null || target is PlayerManager) continue;

            target.Damage(new DamagePacket(blastDamage, DamageTag.Direct, this));
        }

        // 2. the zone it leaves, inheriting a share of the basic attack's burn
        GroundZoneSource.Spawn(
            ctx.Pool,
            FindBasicZoneSource(ctx),
            origin,
            ctx.Values.Damage(P_ZoneDamage),
            ctx.Values.Area(P_ZoneRadius),
            ctx.Values.Get(P_ZoneInherit, 0.4f),
            ctx.Values.Get(P_ZoneTickInterval, 0f),
            this);
    }

    // WHY: the inheritance chain is what makes role-typed slots matter — the basic slot is
    // guaranteed to hold a Basic skill, so this lookup cannot come back empty in a valid loadout.
    // It still tolerates a missing source and simply contributes nothing, so a half-built tank in
    // the editor does not throw.
    private GroundZoneSource FindBasicZoneSource(SkillContext ctx)
    {
        TankSkillLoadout loadout = ctx.Owner.GetComponentInChildren<TankSkillLoadout>();
        if (loadout == null) loadout = ctx.Owner.GetComponentInParent<TankSkillLoadout>();
        if (loadout == null || loadout.Basic == null || loadout.Basic.IsEmpty) return null;

        FireballBasicBehaviour basic = loadout.GetComponentInChildren<FireballBasicBehaviour>(true);
        return basic != null ? basic.ZoneSource : null;
    }
}
