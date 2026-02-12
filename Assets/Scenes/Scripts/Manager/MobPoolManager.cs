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
    public GameObject enemyPrefab;
    private IObjectPool<GeneralEnemy> mobPool;

    [SerializeField] private DropService dropService;

    void Start()
{
    manager = GameManager.Instance;
    player = manager.player;

    // Pre-create 500 mobs so there is no lag during gameplay
    List<GeneralEnemy> tempPrewarmList = new List<GeneralEnemy>();
    
    for (int i = 0; i < 500; i++)
    {
        tempPrewarmList.Add(mobPool.Get());
    }

    // Immediately put them back so they are "Ready" in the pool
    foreach (GeneralEnemy obj in tempPrewarmList)
    {
        mobPool.Release(obj);
    }
}

    void Awake()
    {
        //dropService = manager.gameObject.GetComponent<DropService>();
        mobPool = new ObjectPool<GeneralEnemy>(
            createFunc: () =>
            {
                GameObject obj = Instantiate(enemyPrefab);
                return obj.GetComponent<GeneralEnemy>();
            },

            actionOnGet: (enemy) =>
            {
                enemy.gameObject.SetActive(true);
                enemy.Initialize(dropService, mobPool);
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
            defaultCapacity: 500,
            maxSize: 1000
        );
    }

    public void SpawnMob(Vector3 position)
    {
        GeneralEnemy mob = mobPool.Get();
        mob.transform.position = position;
    }

    public void KillMob(GeneralEnemy mob)
    {
        mobPool.Release(mob);
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
            SpawnMob(spawnPos);

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
