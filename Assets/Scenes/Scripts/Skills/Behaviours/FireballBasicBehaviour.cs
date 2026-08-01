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
    /// Prefab-valued parameter: which round this skill throws. Editable in the Tank Designer.
    public const string P_Projectile = "projectile";

    public const string P_ProjectileCount = "projectileCount";
    public const string P_SpreadAngle = "spreadAngle";
    public const string P_ZoneDamage = "zoneDamagePerTick";
    public const string P_ZoneRadius = "zoneRadius";
    public const string P_ZoneTickInterval = "zoneTickInterval";

    // WHY: kept only as the fallback for a skill that declares no 'projectile' parameter. The round
    // a skill throws belongs in the parameter list, where the Tank Designer can show it and a tank
    // can swap it — a choice buried on this prefab is invisible to the person tuning the game.
    [Tooltip("Fallback round, used only when the skill asset declares no 'projectile' parameter.")]
    [SerializeField] private GameObject _projectilePrefab;

    // WHY: no "leaves burning ground" toggle here any more. There were two switches for one effect —
    // this one, and the round's own — and the round's always won, so turning this off did nothing
    // and the fire looked impossible to stop. Whether a shot leaves fire is now decided in exactly
    // one place: which round is loaded. Want no fire? Load the plain round.
    [Tooltip("Zone this skill imposes on its round, so skill upgrades reach it. Empty = the round decides.")]
    [SerializeField] private GameObject _groundZonePrefab;

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

        GameObject round = ctx.Values.GetObject(P_Projectile, _projectilePrefab);
        if (round == null) return;

        int count = Mathf.Max(1, ctx.Values.GetInt(P_ProjectileCount, 1));
        float spread = ctx.Values.Get(P_SpreadAngle, 0f);
        Quaternion aim = ctx.AimRotation;

        for (int i = 0; i < count; i++)
        {
            float offset = count == 1 ? 0f : spread * (i - (count - 1) / 2f);
            Quaternion rotation = aim * Quaternion.Euler(0f, 0f, offset);

            PooledProjectile shot = ctx.Pool.Spawn(round, ctx.Muzzle.position, rotation);
            if (shot == null) continue;

            // hand over the skill's zone only when it actually has one; otherwise the round keeps
            // its own settings, which is what makes a fire round behave the same whoever fires it
            FireballImpact impact = shot.GetComponent<FireballImpact>();
            if (impact != null) impact.Configure(ZoneSource, ctx.Pool);
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
