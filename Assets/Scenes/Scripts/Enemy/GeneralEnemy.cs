using System.Collections;
using System.Collections.Generic;
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
        // WHY: enemies come back out of a pool, so a stale window from a previous life would make a
        // freshly spawned enemy ignore its first hit.
        _iframes.Clear();

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

    // WHY: invulnerability frames were global to the enemy — ONE window, started by any hit, that
    // blocked everything. Two consequences, both wrong:
    //   1. Several bullets landing together only counted once, so a spread shot or a multi-hit
    //      upgrade did the damage of a single bullet. This is the bug the owner hit.
    //   2. A burning zone ticking on a target that was also being shot had its ticks swallowed.
    // The window now belongs to the SOURCE of the hit — which is what the packet carries a source
    // for. One bullet cannot hit the same enemy twice inside the window; three bullets are three
    // separate hits. Burn is rate-limited by its own tick interval and is exempt entirely.
    public void Damage(DamagePacket packet)
    {
        bool respectsIframe = packet.Tag == DamageTag.Direct;
        object source = packet.Source ?? this;

        if (respectsIframe && IsBlockedFor(source)) return;

        currentHealth -= packet.Amount;
        healthBar.Decrease(packet.Amount);

        if (currentHealth <= 0)
        {
            Dead();
            return;
        }

        if (respectsIframe) StartIframeFor(source);
    }

    private struct IframeWindow
    {
        public object source;
        public float until;
    }

    private readonly List<IframeWindow> _iframes = new List<IframeWindow>();

    private bool IsBlockedFor(object source)
    {
        PruneIframes();

        for (int i = 0; i < _iframes.Count; i++)
            if (ReferenceEquals(_iframes[i].source, source)) return true;

        return false;
    }

    private void StartIframeFor(object source)
    {
        float until = Time.time + iframeDuration;

        for (int i = 0; i < _iframes.Count; i++)
        {
            if (!ReferenceEquals(_iframes[i].source, source)) continue;

            _iframes[i] = new IframeWindow { source = source, until = until };
            isIframe = true;
            return;
        }

        _iframes.Add(new IframeWindow { source = source, until = until });
        isIframe = true;
    }

    private void PruneIframes()
    {
        // WHY: sources are pooled bullets and skill instances, so the list would otherwise grow for
        // the lifetime of the enemy. Expired entries are dropped whenever it is consulted.
        for (int i = _iframes.Count - 1; i >= 0; i--)
            if (Time.time >= _iframes[i].until) _iframes.RemoveAt(i);

        isIframe = _iframes.Count > 0;
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