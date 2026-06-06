using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamagePerTick : GeneralProjectile
{
    [SerializeField] private float _bulletLife;
    //Re-Enable these if i ever need moving fires
    //[SerializeField] private float _speed;
    //[SerializeField] private bool _hasHit;
    // private void Update()
    // {
    //     //transform.Translate(Vector3.right * _speed);
    // }
    private void OnEnable()
    {
        //_hasHit = false;
        StartCoroutine(ActiveCycle(gameObject));
    }
    IEnumerator ActiveCycle(GameObject o)
    {
        yield return new WaitForSeconds(_bulletLife);
        o.SetActive(false);
    }
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
