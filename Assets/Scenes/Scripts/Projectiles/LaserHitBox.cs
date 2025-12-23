using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserHitBox : GeneralProjectile
{
    private void OnTriggerStay2D(Collider2D collision)
    {
        //Debug.Log("Hit"+ collision.tag);
        if (collision.CompareTag("Ground") || collision.CompareTag(tagDamage))
        {
            IDamageable damageable = collision.GetComponent<IDamageable>();
            damageable?.Damage(manager.playerStat.damage);
        }
    }
}
