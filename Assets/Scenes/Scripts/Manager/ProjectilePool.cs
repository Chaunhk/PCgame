using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// One pool per projectile prefab, in the same shape MobPoolManager uses for enemies.
/// Spawners ask for a prefab and get a live instance; the instance returns itself.
/// </summary>
// WHY: bullets and fire zones used to be fixed arrays of scene objects (51 and 11), searched
// linearly for an inactive one on every shot. Two problems that both get worse as skills are
// added: the search cost grows with the array, and when nothing was free the shot silently did
// not happen — no bullet, no error, nothing to notice in a playtest.
public class ProjectilePool : MonoBehaviour
{
    [System.Serializable]
    public struct Entry
    {
        public GameObject prefab;
        [Tooltip("Instances created up front, before the first shot.")]
        public int prewarm;
        [Tooltip("Upper bound on retained instances. 0 = use prewarm x 4.")]
        public int maxSize;
    }

    [SerializeField] private List<Entry> _entries = new List<Entry>();

    private readonly Dictionary<GameObject, IObjectPool<PooledProjectile>> _pools =
        new Dictionary<GameObject, IObjectPool<PooledProjectile>>();

    private Transform _root;

    private void Awake()
    {
        _root = transform;

        foreach (Entry entry in _entries)
        {
            if (entry.prefab == null)
            {
                Debug.LogWarning($"{nameof(ProjectilePool)}: an entry has no prefab assigned; skipping it.");
                continue;
            }
            if (entry.prefab.GetComponent<PooledProjectile>() == null)
            {
                // WHY: without this component the instance has no way back into the pool, so it
                // would be handed out once and leak. Failing loudly at startup beats a slow
                // drip of missing bullets during play.
                Debug.LogError($"{nameof(ProjectilePool)}: prefab '{entry.prefab.name}' has no {nameof(PooledProjectile)} component; it cannot be pooled.");
                continue;
            }

            CreatePool(entry);
        }
    }

    private void CreatePool(Entry entry)
    {
        GameObject prefab = entry.prefab;
        int max = entry.maxSize > 0 ? entry.maxSize : Mathf.Max(4, entry.prewarm * 4);

        var pool = new ObjectPool<PooledProjectile>(
            createFunc: () =>
            {
                GameObject obj = Instantiate(prefab, _root);
                obj.SetActive(false);
                PooledProjectile projectile = obj.GetComponent<PooledProjectile>();
                projectile.BindToPool(this, prefab);
                return projectile;
            },
            actionOnGet: (projectile) =>
            {
                projectile.gameObject.SetActive(true);
                projectile.OnSpawned();
            },
            actionOnRelease: (projectile) =>
            {
                projectile.gameObject.SetActive(false);
            },
            actionOnDestroy: (projectile) =>
            {
                if (projectile != null) Destroy(projectile.gameObject);
            },
            collectionCheck: false,
            defaultCapacity: Mathf.Max(1, entry.prewarm),
            maxSize: max
        );

        _pools.Add(prefab, pool);

        Prewarm(pool, entry.prewarm);
    }

    private static void Prewarm(IObjectPool<PooledProjectile> pool, int count)
    {
        if (count <= 0) return;

        var temp = new List<PooledProjectile>(count);
        for (int i = 0; i < count; i++) temp.Add(pool.Get());
        foreach (PooledProjectile projectile in temp) pool.Release(projectile);
    }

    /// <summary>Spawn <paramref name="prefab"/> at a position and rotation. Null if the prefab is not pooled.</summary>
    public PooledProjectile Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!_pools.TryGetValue(prefab, out IObjectPool<PooledProjectile> pool))
        {
            Debug.LogError($"{nameof(ProjectilePool)}: '{prefab.name}' is not registered. Add it to the pool's list in the inspector.");
            return null;
        }

        PooledProjectile projectile = pool.Get();
        projectile.transform.SetPositionAndRotation(position, rotation);
        return projectile;
    }

    /// <summary>Called by <see cref="PooledProjectile.Despawn"/>. Spawners should not call this.</summary>
    public void Release(GameObject prefabKey, PooledProjectile projectile)
    {
        if (prefabKey != null && _pools.TryGetValue(prefabKey, out IObjectPool<PooledProjectile> pool))
            pool.Release(projectile);
        else
            projectile.gameObject.SetActive(false);
    }
}
