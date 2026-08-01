using UnityEngine;

/// <summary>
/// Leaves a ground zone where the projectile lands. Put this on a delivery round — a bullet whose
/// job is to place fire rather than to hit.
/// </summary>
// WHY: the projectile itself must not know about skills or upgrades — it is a pooled object reused
// by anything. It takes its zone either from the skill that fired it (so upgrades reach it) or from
// its own inspector values (so a fire round works no matter who fires it, including the plain
// shooting path that has no skill behind it at all).
[RequireComponent(typeof(PooledProjectile))]
public class FireballImpact : MonoBehaviour
{
    [Header("Own zone (used when no skill supplies one)")]
    [Tooltip("Ground zone to leave behind. Empty means this bullet leaves nothing.")]
    [SerializeField] private GameObject _zonePrefab;
    [SerializeField] private int _damagePerTick = 3;
    [SerializeField] private float _radius = 2f;
    [SerializeField] private float _tickInterval = 0.5f;

    [Tooltip("Also leave the zone when the bullet expires without hitting anything.")]
    [SerializeField] private bool _alsoOnExpire;

    private GroundZoneSource _skillZone;
    private ProjectilePool _pool;
    private bool _spent;

    /// Called by the skill that fired this shot, so upgrades to the skill reach the zone.
    public void Configure(GroundZoneSource zoneSource, ProjectilePool pool)
    {
        _skillZone = zoneSource;
        _pool = pool;
        _spent = false;
    }

    private void OnEnable()
    {
        // WHY: pooled instances come back reused. Without this reset, a bullet that already left its
        // zone once would refuse to leave another on its next flight.
        _spent = false;
    }

    /// Called by Bullet the moment it connects.
    public void NotifyHit()
    {
        SpawnZone();
    }

    private void OnDisable()
    {
        if (_alsoOnExpire) SpawnZone();
    }

    private void SpawnZone()
    {
        if (_spent) return;

        ProjectilePool pool = ResolvePool();
        if (pool == null) return;

        if (_skillZone != null && _skillZone.Prefab != null)
        {
            _spent = true;
            GroundZoneSource.Spawn(
                pool, _skillZone, transform.position,
                _skillZone.Damage, _skillZone.Radius,
                0f,                     // this IS the owner's zone — nothing to inherit
                _skillZone.TickInterval, _skillZone);
            return;
        }

        if (_zonePrefab == null) return;

        _spent = true;

        PooledProjectile zone = pool.Spawn(_zonePrefab, transform.position, Quaternion.identity);
        if (zone == null) return;

        zone.transform.localScale = new Vector3(_radius, _radius, 1f);

        DamagePerTick tick = zone.GetComponent<DamagePerTick>();
        if (tick != null) tick.Configure(_damagePerTick, _tickInterval, this);
    }

    private ProjectilePool ResolvePool()
    {
        if (_pool != null) return _pool;

        GameManager manager = GameManager.Instance;
        _pool = manager != null ? manager.projectilePool : null;
        return _pool;
    }
}
