using UnityEngine;

/// <summary>
/// Basic attack: throws a fireball toward the cursor. Optionally leaves a burning zone where it
/// lands once the first upgrade is on.
/// </summary>
// WHY: this is the shooting that used to live inside ShootPointController with its own timer and
// its own pool scan. As a skill it gets its rate, its spread and its projectile count from the
// slot's resolved numbers, so the same skill on a heavier tank can fire slower without a code path.
public class FireballBasicBehaviour : SkillBehaviour
{
    public const string P_ProjectileCount = "projectileCount";
    public const string P_SpreadAngle = "spreadAngle";
    public const string P_ZoneDamage = "zoneDamagePerTick";
    public const string P_ZoneRadius = "zoneRadius";
    public const string P_ZoneTickInterval = "zoneTickInterval";

    [Tooltip("The projectile this throws. Must be registered with the projectile pool.")]
    [SerializeField] private GameObject _projectilePrefab;

    [Tooltip("Ground zone left behind. Leave empty until the burning-ground upgrade is unlocked.")]
    [SerializeField] private GameObject _groundZonePrefab;

    [Tooltip("Set by the burning-ground upgrade. Off by default, so the base attack is just a fireball.")]
    [SerializeField] private bool _leavesBurningGround;

    /// The zone this skill owns. Other skills inherit a percentage of it rather than defining
    /// their own burn — see GroundZoneSource.
    public GroundZoneSource ZoneSource { get; private set; }

    public override void OnEquip(SkillContext ctx)
    {
        ZoneSource = new GroundZoneSource(_groundZonePrefab);
        RefreshZoneSource(ctx);
    }

    public override void OnUnequip(SkillContext ctx)
    {
        ZoneSource = null;
    }

    public override void OnActivate(SkillContext ctx)
    {
        RefreshZoneSource(ctx);

        int count = Mathf.Max(1, ctx.Values.GetInt(P_ProjectileCount, 1));
        float spread = ctx.Values.Get(P_SpreadAngle, 0f);
        Quaternion aim = ctx.AimRotation;

        for (int i = 0; i < count; i++)
        {
            float offset = count == 1 ? 0f : spread * (i - (count - 1) / 2f);
            Quaternion rotation = aim * Quaternion.Euler(0f, 0f, offset);

            PooledProjectile shot = ctx.Pool.Spawn(_projectilePrefab, ctx.Muzzle.position, rotation);
            if (shot == null) continue;

            // the fireball drops the zone where it dies, if this skill is upgraded to do that
            FireballImpact impact = shot.GetComponent<FireballImpact>();
            if (impact != null) impact.Configure(_leavesBurningGround ? ZoneSource : null, ctx.Pool);
        }
    }

    private void RefreshZoneSource(SkillContext ctx)
    {
        if (ZoneSource == null) return;

        ZoneSource.Set(
            ctx.Values.Damage(P_ZoneDamage),
            ctx.Values.Area(P_ZoneRadius),
            ctx.Values.Get(P_ZoneTickInterval, 0.5f));
    }
}
