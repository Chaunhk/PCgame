using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class GeneralEnemy : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    public int maxHealth;
    public int currentHealth;
    public int speed;
    public float iframeDuration;
    public float minDistance;

    [Header("Runtime")]
    public bool isIframe;
    public Vector3 dir;

    [Header("References")]
    public EnemyStatSO enemyStat;
    public GeneralBar healthBar;

    protected GameManager manager;
    protected LevelManager levelManager;

    private DropService dropService;
    private IObjectPool<GeneralEnemy> pool;

    // Called from MobPoolManager when taken from pool
    public void Initialize(DropService service, IObjectPool<GeneralEnemy> poolRef)
    {
        dropService = service;
        pool = poolRef;

        manager = GameManager.Instance;
        levelManager = manager.levelManager;

        StopAllCoroutines();
        isIframe = false;

        InitStat();
    }

    private void InitStat()
    {
        float mod = 1 + levelManager.statScale * levelManager.level;

        maxHealth = (int)(enemyStat.maxHealth * mod);
        currentHealth = maxHealth;

        speed = enemyStat.speed;
        minDistance = manager.minDistance;

        healthBar.InitData(maxHealth);
    }

    private void Update()
    {
        if (manager == null || manager.player == null) return;

        Vector3 direction = (manager.player.transform.position - transform.position).normalized;

        if (Vector3.Distance(transform.position, manager.player.transform.position) > minDistance)
        {
            transform.position += direction * speed * Time.deltaTime;
        }
    }

    public void Damage(int damageAmount)
    {
        if (isIframe) return;

        currentHealth -= damageAmount;
        healthBar.Decrease(damageAmount);

        if (currentHealth <= 0)
        {
            Dead();
            return;
        }

        StartCoroutine(ImuneToDamage());
    }

    IEnumerator ImuneToDamage()
    {
        isIframe = true;
        yield return new WaitForSeconds(iframeDuration);
        isIframe = false;
    }

    public void Dead()
    {
        // Spawn drops BEFORE releasing to pool
        Drop();
        DeSpawn();
    }
    public void DeSpawn()
    {
         manager.enemyCount--;

        if (manager.enemyCount == 0 && manager.isSpawnEnd)
            manager.eventControl.PassLevel();

        // Return to pool (DO NOT disable manually)
        pool.Release(this);
    }
    private void Drop()
    {
        if (enemyStat.dropTable != null)
        {
            dropService.SpawnDrops(enemyStat.dropTable, transform.position);
        }
    }
}