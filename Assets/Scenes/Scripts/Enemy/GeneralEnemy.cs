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

    // WHY: invulnerability frames used to block EVERY kind of damage, and any hit started them.
    // A burning zone ticking on a target that is also being shot had almost all of its ticks
    // swallowed — the fire kit is built on burn zones, so its upgrades would have felt like they
    // did nothing, for a reason no playtest would surface. Burn is rate-limited by its own tick
    // interval and does not need contact immunity; only Direct hits do.
    public void Damage(DamagePacket packet)
    {
        bool respectsIframe = packet.Tag == DamageTag.Direct;
        if (respectsIframe && isIframe) return;

        currentHealth -= packet.Amount;
        healthBar.Decrease(packet.Amount);

        if (currentHealth <= 0)
        {
            Dead();
            return;
        }

        if (respectsIframe) StartCoroutine(ImuneToDamage());
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