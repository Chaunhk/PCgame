using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : GeneralProjectile
{
    [SerializeField] private float _bulletLife;
    [SerializeField] private float _speed;
    [SerializeField] private bool _hasHit;
    private void Update()
    {
        transform.Translate(Vector3.right * _speed);
    }
    private void OnEnable()
    {
        _hasHit = false;
        StartCoroutine(ActiveCycle(gameObject));
    }
    IEnumerator ActiveCycle(GameObject o)
    {
        yield return new WaitForSeconds(_bulletLife);
        o.SetActive(false);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hasHit) return;//prevent multiple Collider hit, change to number to control piercing i guess
        Debug.Log("Hit"+ collision.tag);
        if (collision.CompareTag("Ground") || collision.CompareTag(tagDamage))
        {
            IDamageable damageable = collision.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.Damage(manager.playerStat.damage);
                
            }
            _hasHit = true;
            gameObject.SetActive(false);
        }
    }
}
