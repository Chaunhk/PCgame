using UnityEngine;

/// <summary>
/// The burning zone, as one thing owned by one skill and borrowed by the others.
/// </summary>
// WHY: three entries in the kit end with the same sentence — the zone inherits from the basic
// attack's first upgrade. If each skill spawned its own private zone, that sentence would be
// implemented three times and every later tuning change to "the burn" would have to be made in
// three places that then drift. The zone lives here once; other skills take a percentage of it.
public class GroundZoneSource
{
    public GameObject Prefab { get; private set; }

    /// Damage per tick of the owner's own zone, before anyone inherits from it.
    public int Damage { get; private set; }
    /// Radius of the owner's own zone.
    public float Radius { get; private set; }
    public float TickInterval { get; private set; }

    public GroundZoneSource(GameObject prefab)
    {
        Prefab = prefab;
        TickInterval = 0.5f;
    }

    public void Set(int damage, float radius, float tickInterval)
    {
        Damage = damage;
        Radius = radius;
        TickInterval = Mathf.Max(0.05f, tickInterval);
    }

    /// <summary>
    /// Spawn a zone that has its own base numbers plus a share of the owner's.
    /// </summary>
    /// <param name="inherit">0 = nothing inherited, 0.4 = the "30~50%" the kit asks for.</param>
    public static PooledProjectile Spawn(
        ProjectilePool pool,
        GroundZoneSource source,
        Vector3 position,
        int ownDamage,
        float ownRadius,
        float inherit,
        float tickInterval,
        object owner)
    {
        if (pool == null) return null;

        GameObject prefab = source != null ? source.Prefab : null;
        if (prefab == null) return null;

        int damage = ownDamage + Mathf.RoundToInt(inherit * (source != null ? source.Damage : 0));
        float radius = ownRadius + inherit * (source != null ? source.Radius : 0f);
        if (tickInterval <= 0f) tickInterval = source != null ? source.TickInterval : 0.5f;

        PooledProjectile zone = pool.Spawn(prefab, position, Quaternion.identity);
        if (zone == null) return null;

        zone.transform.localScale = new Vector3(radius, radius, 1f);

        DamagePerTick tick = zone.GetComponent<DamagePerTick>();
        if (tick != null) tick.Configure(damage, tickInterval, owner);

        return zone;
    }
}
