using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class MobPoolManager : MonoBehaviour
{
    private GameObject player;
    private GameManager manager;
    public float innerRadius;
    public float outerRadius;
    public List<GameObject> enemyPrefabs; // changed from single to list

    private Dictionary<GameObject, IObjectPool<GeneralEnemy>> mobPools;

    [SerializeField] private DropService dropService;

    void Awake()
    {
        mobPools = new Dictionary<GameObject, IObjectPool<GeneralEnemy>>();

        foreach (GameObject prefab in enemyPrefabs)
        {
            GameObject capturedPrefab = prefab;

            var pool = new ObjectPool<GeneralEnemy>(
                createFunc: () =>
                {
                    GameObject obj = Instantiate(capturedPrefab);
                    return obj.GetComponent<GeneralEnemy>();
                },
                actionOnGet: (enemy) =>
                {
                    enemy.gameObject.SetActive(true);
                    enemy.Initialize(dropService, mobPools[capturedPrefab]);
                },
                actionOnRelease: (enemy) =>
                {
                    enemy.gameObject.SetActive(false);
                },
                actionOnDestroy: (enemy) =>
                {
                    Destroy(enemy.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: 500 / enemyPrefabs.Count,
                maxSize: 1000 / enemyPrefabs.Count
            );

            mobPools.Add(capturedPrefab, pool);
        }
    }

    void Start()
    {
        manager = GameManager.Instance;
        player = manager.player;

        // Prewarm each pool evenly
        int prewarmPerPool = 500 / enemyPrefabs.Count;

        foreach (var kvp in mobPools)
        {
            List<GeneralEnemy> temp = new List<GeneralEnemy>();

            for (int i = 0; i < prewarmPerPool; i++)
                temp.Add(kvp.Value.Get());

            foreach (GeneralEnemy obj in temp)
                kvp.Value.Release(obj);
        }
    }

    public void SpawnMob(Vector3 position)
    {
        // Pick a random prefab and get from its pool
        int randomIndex = Random.Range(0, enemyPrefabs.Count);
        GameObject randomPrefab = enemyPrefabs[randomIndex];

        GeneralEnemy mob = mobPools[randomPrefab].Get();
        mob.transform.position = position;
    }

    public void KillMob(GeneralEnemy mob)
    {
        mob.DeSpawn();
    }
    public Vector3 GetRandomSpawnPoint()
    {
        // 1. Get a random direction (normalized vector)
        Vector2 randomDirection = Random.insideUnitCircle.normalized;

        // 2. Pick a random distance between your two radii
        float randomDistance = Random.Range(innerRadius, outerRadius);

        // 3. Combine with player position so the ring follows the player
        Vector3 spawnOffset = new Vector3(randomDirection.x, randomDirection.y, 0) * randomDistance;
        return player.transform.position + spawnOffset;
    }
    [Header("Spawn Settings")]
    public float spawnInterval = 1.0f; // Time between spawns (seconds)
    private float timer;

    void Update()
    {
        // Don't spawn if player isn't assigned yet
        if (player == null) return;

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            // 1. Get the random point from your existing method
            Vector3 spawnPos = GetRandomSpawnPoint();

            // 2. Spawn the mob using your pool
            //SpawnMob(spawnPos);

            // 3. Reset timer
            timer = 0;
            
            // Optional: Make it harder over time
            // spawnInterval = Mathf.Max(0.1f, spawnInterval - 0.001f); 
        }
    }
    [Header("Visualization")]
    public bool showGizmos = true;
    public int circleSegments = 32; // Higher number = smoother circle

    private void OnDrawGizmos()
    {
        if (!showGizmos || player == null) return;

        // Set the color for the inner circle (where mobs SHOULD NOT spawn)
        Gizmos.color = Color.red;
        DrawCircle(player.transform.position, innerRadius);

        // Set the color for the outer circle (the maximum spawn distance)
        Gizmos.color = Color.green;
        DrawCircle(player.transform.position, outerRadius);
    }

    // Helper method to draw a circle using lines
    private void DrawCircle(Vector3 center, float radius)
    {
        float angleStep = 360f / circleSegments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= circleSegments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
            
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
    }

}
