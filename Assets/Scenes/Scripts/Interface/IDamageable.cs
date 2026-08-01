using UnityEngine;

/// <summary>How a hit was delivered. Decides which rules apply to it, i-frames above all.</summary>
public enum DamageTag
{
    /// A discrete hit: a bullet, a melee contact, an explosion. Subject to invulnerability frames.
    Direct,
    /// Damage over time from standing in something. Rate-limited by its own tick interval.
    Burn,
}

/// <summary>
/// One hit, with enough context to answer "who did this, and how".
/// </summary>
// WHY: Damage(int) could not say where a hit came from, which blocks anything that reacts to the
// source of damage rather than the fact of it — a mark that explodes for the values it was applied
// with, a passive that triggers on burn but not on bullets, a kill credited to a skill.
public readonly struct DamagePacket
{
    public readonly int Amount;
    public readonly DamageTag Tag;
    /// The skill, module or object responsible. May be null for unattributed damage.
    public readonly object Source;

    public DamagePacket(int amount, DamageTag tag = DamageTag.Direct, object source = null)
    {
        Amount = amount;
        Tag = tag;
        Source = source;
    }
}

public interface IDamageable
{
    void Damage(DamagePacket packet);
    void Dead();
}

public static class DamageableExtensions
{
    /// Convenience for plain direct hits with no source to report.
    public static void Damage(this IDamageable target, int amount)
    {
        target.Damage(new DamagePacket(amount));
    }
}
