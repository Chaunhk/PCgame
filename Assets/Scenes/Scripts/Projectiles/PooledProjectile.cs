using UnityEngine;

/// <summary>
/// Marks a projectile as pool-owned and gives it one way to go away: Despawn().
/// Attach to any prefab handed out by <see cref="ProjectilePool"/>.
/// </summary>
// WHY: projectiles used to end their life with gameObject.SetActive(false), which meant the
// spawner had to find a disabled one by scanning an array, and an object disabled by any other
// path silently became "available". Ownership now runs one way — the pool hands it out, the
// projectile hands itself back — so there is no scan and no ambiguity about who is free.
[DisallowMultipleComponent]
public class PooledProjectile : MonoBehaviour
{
    [Tooltip("Seconds before this returns to the pool on its own. 0 or less = no timeout.")]
    [SerializeField] private float _lifetime = 3f;

    private ProjectilePool _pool;
    private GameObject _prefabKey;
    private float _despawnAt;
    private bool _isLive;

    public float Lifetime => _lifetime;

    /// Called by the pool when this instance is created. Not called per spawn.
    public void BindToPool(ProjectilePool pool, GameObject prefabKey)
    {
        _pool = pool;
        _prefabKey = prefabKey;
    }

    /// Called by the pool every time this instance is handed out.
    public void OnSpawned()
    {
        _isLive = true;
        _despawnAt = _lifetime > 0f ? Time.time + _lifetime : float.PositiveInfinity;
    }

    private void Update()
    {
        // WHY: the old lifetime was a coroutine started in OnEnable. Coroutines do not survive
        // the object being disabled, so a bullet that was recycled mid-flight could come back
        // with no timer running and live until it happened to hit something.
        if (_isLive && Time.time >= _despawnAt) Despawn();
    }

    public void Despawn()
    {
        if (!_isLive) return;   // guard against a double release from hit + timeout on one frame
        _isLive = false;

        if (_pool != null) _pool.Release(_prefabKey, this);
        else gameObject.SetActive(false);   // not pooled (scene-placed instance) — old behaviour
    }
}
