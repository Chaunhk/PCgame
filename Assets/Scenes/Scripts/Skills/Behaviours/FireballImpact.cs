using UnityEngine;

/// <summary>
/// Sits on the fireball projectile. Drops a burning zone where the shot ends, when the skill that
/// fired it is upgraded to do so.
/// </summary>
// WHY: the projectile itself must not know about skills or upgrades — it is a pooled object reused
// by anything. It is handed a zone source per shot instead, and does nothing when handed none.
[RequireComponent(typeof(PooledProjectile))]
public class FireballImpact : MonoBehaviour
{
    private GroundZoneSource _zoneSource;
    private ProjectilePool _pool;
    private bool _spent;

    public void Configure(GroundZoneSource zoneSource, ProjectilePool pool)
    {
        _zoneSource = zoneSource;
        _pool = pool;
        _spent = false;
    }

    private void OnDisable()
    {
        // WHY: the fireball returns to the pool on hit and on timeout, and both should leave the
        // zone — the kit says "enemies hit or burning", not "enemies hit". Guarding on _spent keeps
        // a single disable from dropping two zones if something disables it twice in a frame.
        if (_spent || _zoneSource == null || _pool == null) return;
        _spent = true;

        GroundZoneSource.Spawn(
            _pool,
            _zoneSource,
            transform.position,
            _zoneSource.Damage,
            _zoneSource.Radius,
            0f,                       // the owner's zone: nothing to inherit, it IS the source
            _zoneSource.TickInterval,
            _zoneSource);
    }
}
