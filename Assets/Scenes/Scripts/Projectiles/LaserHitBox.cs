using System.Collections.Generic;
using UnityEngine;

/// <summary>The damaging body of the laser beam. Same tick model as a ground zone.</summary>
// WHY: this damaged on every OnTriggerStay2D and depended on the target's invulnerability frames
// to throttle it. Burn-tagged damage no longer triggers those frames, so the beam owns its rate.
public class LaserHitBox : GeneralProjectile
{
    [SerializeField] private float _tickInterval = 0.2f;

    private readonly List<IDamageable> _occupants = new List<IDamageable>();
    private float _nextTickAt;

    private void OnEnable()
    {
        _occupants.Clear();
        _nextTickAt = Time.time;
    }

    private void Update()
    {
        if (Time.time < _nextTickAt) return;
        _nextTickAt = Time.time + Mathf.Max(0.05f, _tickInterval);

        for (int i = _occupants.Count - 1; i >= 0; i--)
        {
            IDamageable occupant = _occupants[i];
            if (occupant == null || (occupant as MonoBehaviour) == null || !((MonoBehaviour)occupant).gameObject.activeInHierarchy)
            {
                _occupants.RemoveAt(i);
                continue;
            }

            occupant.Damage(new DamagePacket(manager.playerStat.spDamage, DamageTag.Burn, this));
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
