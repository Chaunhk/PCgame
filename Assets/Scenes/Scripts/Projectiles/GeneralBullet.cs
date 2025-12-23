using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralBullet : MonoBehaviour
{
    public enum BulletType
    {
        playerBullet,
        enemyBullet
    }
    private GameManager manager;
    [SerializeField] private string tagDamage = "";
    [SerializeField] private BulletType _type;
    [SerializeField] private float _bulletLife;
    public float speed;
    public float i;
    private void Start()
    {
        manager = GameManager.Instance;
        switch (_type)
        {
            case (BulletType.enemyBullet):
                {
                    tagDamage = "Player";
                    break;
                }
            case (BulletType.playerBullet):
                {
                    tagDamage = "Enemy";
                    break;
                }
        }
        //Debug.Log(tagDamage);
    }
    
    void Update()
    {
        transform.Translate(Vector3.right * speed * i);
    }
    private void OnEnable()
    {
        StartCoroutine(ActiveCycle(gameObject));
    }
    IEnumerator ActiveCycle(GameObject o)
    {
        yield return new WaitForSeconds(_bulletLife);
        o.SetActive(false);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Hit"+ collision.tag);
        if (collision.CompareTag("Ground") || collision.CompareTag(tagDamage))
        {
            IDamageable damageable = collision.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.Damage(manager.playerStat.damage);
                
            }
            gameObject.SetActive(false);
        }
    }
}
