using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PooledProjectile))]
public class Bullet : GeneralProjectile
{
    [SerializeField] private float _speed;

    // WHY: a delivery round exists — a bullet whose whole job is to put something on the ground
    // where it lands. Its damage must come from what it leaves behind, not from the impact, or the
    // same shot would be paid for twice and the zone's numbers would stop being the real ones.
    [Tooltip("Off for a bullet that only delivers an effect: it still stops on contact, but the hit itself does nothing.")]
    [SerializeField] private bool _dealsContactDamage = true;

    private PooledProjectile _pooled;
    private FireballImpact _impact;

    protected virtual void Awake()
    {
        _pooled = GetComponent<PooledProjectile>();
        _impact = GetComponent<FireballImpact>();
    }

    private void Update()
    {
        // WHY: this was Translate(Vector3.right * _speed) with no Time.deltaTime, so bullets
        // moved a fixed distance PER FRAME — twice as fast at 120 fps as at 60, and slower
        // whenever the frame rate dipped. Range and travel time were tied to hardware.
        // The serialized _speed values were rescaled by 60 when this changed, so the speed at
        // 60 fps is unchanged; _speed now reads as units per second.
        transform.Translate(_speed * Time.deltaTime * Vector3.right);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Ground") || collision.CompareTag(tagDamage))
        {
            if (_dealsContactDamage)
            {
                IDamageable damageable = collision.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.Damage(new DamagePacket(manager.playerStat.damage, DamageTag.Direct, this));
                }
            }

            if (_impact != null) _impact.NotifyHit();

            OnHit();
            _pooled.Despawn();
        }
    }

    /// <summary>Hook for subclasses that leave something behind on impact. Runs before despawn.</summary>
    // WHY: this was called DisableBullet, which described neither when it runs nor what it is
    // for — Canon used it to spawn its fire zone, i.e. the one thing it does. Despawning is now
    // the pool's job, so the hook keeps only its real meaning.
    protected virtual void OnHit()
    {
    }
}
