using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// WHY: the lifetime coroutine that used to live here now belongs to PooledProjectile, so a
// fire zone returns to the pool instead of just switching itself off — and its timer no longer
// depends on a coroutine that dies with the object.
[RequireComponent(typeof(PooledProjectile))]
public class DamagePerTick : GeneralProjectile
{
    private void OnTriggerStay2D(Collider2D collision)
    {
        //Debug.Log("Hit"+ collision.tag);
        if (collision.CompareTag("Ground") || collision.CompareTag(tagDamage))
        {
            IDamageable damageable = collision.GetComponent<IDamageable>();
            damageable?.Damage(manager.playerStat.spDamage);
        }
    }
}
