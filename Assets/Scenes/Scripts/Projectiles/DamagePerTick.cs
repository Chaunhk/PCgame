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
    private bool _seeded;

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
        _seeded = false;
        _nextTickAt = Time.time;   // first tick lands immediately, so walking in hurts at once
    }

    // WHY: the occupant list was filled only by OnTriggerEnter2D, and a zone that appears ON TOP of
    // something never gets an enter event for it — the target did not move, and the zone is a static
    // collider being repositioned. So an enemy standing exactly where the shot landed took no damage
    // at all, while anyone who wandered in afterwards burned normally. The zone now asks physics who
    // is already inside it, once, on its first tick. It cannot run in OnEnable: the pool enables the
    // object before it moves it into place, so at that moment it is still at its previous position.
    private void SeedOccupants()
    {
        _seeded = true;

        CircleCollider2D area = GetComponent<CircleCollider2D>();
        if (area == null) return;

        float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        Vector2 center = (Vector2)transform.TransformPoint(area.offset);

        Collider2D[] inside = Physics2D.OverlapCircleAll(center, area.radius * scale);
        for (int i = 0; i < inside.Length; i++)
        {
            Collider2D other = inside[i];
            if (!other.CompareTag("Ground") && !other.CompareTag(tagDamage)) continue;

            IDamageable damageable = other.GetComponent<IDamageable>();
            if (damageable != null && !_occupants.Contains(damageable)) _occupants.Add(damageable);
        }
    }

    private void Update()
    {
        if (!_seeded) SeedOccupants();

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
