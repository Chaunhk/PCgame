using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralProjectile : MonoBehaviour
{
    protected enum BulletType
    {
        playerBullet,
        enemyBullet
    }
    protected GameManager manager;
    protected string tagDamage = "";
    protected static BulletType type;
    [SerializeField] protected PlayerController playerController;
    protected virtual void Start()
    {
        manager = GameManager.Instance;
        switch (type)
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
        playerController = manager.player.GetComponent<PlayerController>();
    }
    
}
