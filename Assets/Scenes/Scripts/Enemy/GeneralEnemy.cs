using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GeneralEnemy : MonoBehaviour, IDamageable
{
    public int maxHealth;
    public int currentHealth;
    public int speed;
    public float iframeDuration;
    public bool isIframe;
    public Vector3 dir;
    public float minDistance;
    protected GameManager manager;
    public EnemyStatSO enemyStat;
    public GeneralBar healthBar;
    protected LevelManager levelManager;
    private void Start()
    {
        manager = GameManager.Instance;
        levelManager= manager.levelManager;
        InitStat();
        //gameObject.SetActive(false);
    }
    //private void FixedUpdate()
    //{
    //    if (Vector3.Distance(transform.position,manager.player.gameObject.transform.position) > minDistance)
    //        transform.Translate(dir * speed * 0.01f);
    //}
    private void InitStat()
    {
        float mod = 1+levelManager.statScale*levelManager.level;
        maxHealth = (int)(enemyStat.maxHealth*mod);
        currentHealth = maxHealth;
        minDistance = manager.minDistance;
        speed = enemyStat.speed;
        dir = manager.player.transform.position - transform.position;
        healthBar.InitData(maxHealth);
    }
    public void Damage(int damageAmount)
    {
        if(!isIframe) {
            currentHealth -= damageAmount;
            healthBar.Decrease(damageAmount);
            if (currentHealth <= 0)
            {
                Dead();
                return;
            }
            StartCoroutine(ImuneToDamage());
        }
    }
    IEnumerator ImuneToDamage(){
        isIframe = true;
        yield return new WaitForSeconds(iframeDuration);
        isIframe = false;
    }
    public void Dead()
    {
        gameObject.SetActive(false);
        manager.enemyCount--;
        if(manager.enemyCount==0&&manager.isSpawnEnd==true)
            manager.eventControl.PassLevel();
    }
}
