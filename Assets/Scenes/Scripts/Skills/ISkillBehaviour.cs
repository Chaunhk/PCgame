using UnityEngine;

/// <summary>
/// The whole surface a new skill has to implement. No mana, no cooldown, no HUD — the slot owns
/// those, once, for every skill.
/// </summary>
public interface ISkillBehaviour
{
    /// Called once when the skill enters a slot. A Passive does all of its work here.
    void OnEquip(SkillContext ctx);

    /// Called once when the skill leaves a slot. A Passive undoes its work here.
    // WHY: paired with OnEquip on purpose. Slots are swappable, so every effect a skill applies has
    // to be removable — a passive that adds armour on equip and forgets to remove it on unequip
    // turns swapping into a stat exploit. Making the pair mandatory stops that being writable.
    void OnUnequip(SkillContext ctx);

    /// Instant: do the effect. Channeled/Toggle: start it. Passive never receives this.
    void OnActivate(SkillContext ctx);

    /// Channeled/Toggle only, every frame while running.
    void OnTick(SkillContext ctx, float deltaTime);

    /// Channel released, mana ran out, toggled off, or the run ended.
    void OnDeactivate(SkillContext ctx);

    /// Lets a behaviour end itself early — beam spent, ammo gone.
    bool WantsToStop { get; }
}

/// <summary>Base class so a behaviour only writes the parts it uses.</summary>
public abstract class SkillBehaviour : MonoBehaviour, ISkillBehaviour
{
    public virtual void OnEquip(SkillContext ctx) { }
    public virtual void OnUnequip(SkillContext ctx) { }
    public virtual void OnActivate(SkillContext ctx) { }
    public virtual void OnTick(SkillContext ctx, float deltaTime) { }
    public virtual void OnDeactivate(SkillContext ctx) { }
    public virtual bool WantsToStop => false;
}
