using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A ground zone that damages whatever stands in it, on its own tick interval.
/// </summary>
// WHY: this used to damage on every OnTriggerStay2D, i.e. every physics frame (~50/s), and relied
// on the target's invulnerability frames to throttle it. That coupling is now gone — burn damage is
// exempt from i-frames — so the zone has to own its rate itself. It tracks who is standing in it
// and applies one tick to everyone on the interval.
[RequireComponent(typeof(PooledProjectile))]
public class DamagePerTick : GeneralProjectile
{
    [SerializeField] private float _tickInterval = 0.5f;
    [SerializeField] private int _damagePerTick = -1;   // -1 = fall back to the global skill damage

    private readonly List<IDamageable> _occupants = new List<IDamageable>();
    private float _nextTickAt;
    private object _source;

    /// Called by whatever spawns the zone. Skills use this to give a zone its own numbers.
    public void Configure(int damagePerTick, float tickInterval, object source)
    {
        _damagePerTick = damagePerTick;
        _tickInterval = Mathf.Max(0.05f, tickInterval);
        _source = source;
    }

    private void OnEnable()
    {
        _occupants.Clear();
        _nextTickAt = Time.time;   // first tick lands immediately, so walking in hurts at once
    }

    private void Update()
    {
        if (Time.time < _nextTickAt) return;
        _nextTickAt = Time.time + _tickInterval;

        int amount = _damagePerTick >= 0 ? _damagePerTick : manager.playerStat.spDamage;

        // iterate backwards: an occupant that dies on its tick is removed inside the loop
        for (int i = _occupants.Count - 1; i >= 0; i--)
        {
            IDamageable occupant = _occupants[i];
            if (occupant == null || (occupant as MonoBehaviour) == null || !((MonoBehaviour)occupant).gameObject.activeInHierarchy)
            {
                _occupants.RemoveAt(i);
                continue;
            }

            occupant.Damage(new DamagePacket(amount, DamageTag.Burn, _source ?? this));
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Ground") && !collision.CompareTag(tagDamage)) return;

        IDamageable damageable = collision.GetComponent<IDamageable>();
        if (damageable != null && !_occupants.Contains(damageable)) _occupants.Add(damageable);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        IDamageable damageable = collision.GetComponent<IDamageable>();
        if (damageable != null) _occupants.Remove(damageable);
    }
}
