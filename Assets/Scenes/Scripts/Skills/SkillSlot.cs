using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One of the tank's three slots. Owns everything that is the same for every skill: affordability,
/// cooldown, charges, channel bookkeeping, and telling the HUD when any of it changed.
/// </summary>
// WHY: the two skills that existed each re-implemented mana check, mana spend, cooldown check and
// cooldown start, and had already drifted — one started its cooldown on press, the other on release,
// one paused mana regen and the other did not. Writing the rule once is the point of the slot.
public class SkillSlot
{
    public SkillRole Role { get; private set; }
    public SkillDefinitionSO Definition { get; private set; }
    public SkillRuntimeValues Values { get; private set; }

    public int ChargesLeft { get; private set; }
    public float CooldownRemaining { get; private set; }
    public bool IsRunning { get; private set; }

    public bool IsEmpty => Definition == null;
    public bool IsPassive => Definition != null && Definition.activation == SkillActivation.Passive;
    public bool IsReady => !IsEmpty && !IsPassive && !IsRunning && ChargesLeft > 0;

    /// Raised whenever charges, cooldown or running state changed — the HUD listens instead of polling.
    public event Action<SkillSlot> Changed;

    private ISkillBehaviour _behaviour;
    private GameObject _behaviourObject;
    private float _nextChannelTickAt;

    public SkillSlot(SkillRole role)
    {
        Role = role;
    }

    public void Equip(SkillDefinitionSO definition, IList<ParameterOverride> overrides, SkillContext ctx, Transform parent)
    {
        Unequip(ctx);

        if (definition == null) return;

        if (definition.role != Role)
        {
            // WHY: role is what guarantees a tank always has a basic attack, which the fire kit's
            // "inherits from the basic attack" chain depends on. Refusing here means an invalid
            // loadout cannot reach play mode and quietly behave oddly.
            Debug.LogError($"Skill '{definition.skillId}' is a {definition.role} skill and cannot go in the {Role} slot.");
            return;
        }

        Definition = definition;
        Values = new SkillRuntimeValues();
        Values.Resolve(definition, overrides, ctx.Stats, ctx.BaseStats);

        ChargesLeft = Values.MaxCharges;
        CooldownRemaining = 0f;
        IsRunning = false;

        if (definition.behaviourPrefab != null)
        {
            _behaviourObject = UnityEngine.Object.Instantiate(definition.behaviourPrefab, parent);
            _behaviourObject.name = $"Skill_{definition.skillId}";
            _behaviour = _behaviourObject.GetComponent<ISkillBehaviour>();

            if (_behaviour == null)
                Debug.LogError($"Skill '{definition.skillId}': behaviour prefab '{definition.behaviourPrefab.name}' has no {nameof(ISkillBehaviour)} component.");
        }

        if (_behaviour != null)
        {
            ctx.Values = Values;
            _behaviour.OnEquip(ctx);
        }

        Changed?.Invoke(this);
    }

    public void Unequip(SkillContext ctx)
    {
        if (Definition == null) return;

        if (_behaviour != null)
        {
            ctx.Values = Values;
            if (IsRunning) _behaviour.OnDeactivate(ctx);
            _behaviour.OnUnequip(ctx);
        }

        if (_behaviourObject != null) UnityEngine.Object.Destroy(_behaviourObject);

        _behaviour = null;
        _behaviourObject = null;
        Definition = null;
        Values = null;
        IsRunning = false;
        ChargesLeft = 0;
        CooldownRemaining = 0f;

        Changed?.Invoke(this);
    }

    public bool TryActivate(SkillContext ctx)
    {
        if (!IsReady) return false;
        if (!ctx.Player.ConsumeMana(Values.Cost)) return false;

        ChargesLeft--;
        if (CooldownRemaining <= 0f) CooldownRemaining = Values.Cooldown;

        ctx.Values = Values;
        _behaviour?.OnActivate(ctx);

        if (Definition.activation == SkillActivation.Instant)
        {
            ctx.Player.OnSkillEnd();
        }
        else
        {
            IsRunning = true;
            _nextChannelTickAt = Time.time + Definition.tickInterval;
            ctx.Player.OnSkillStart();
        }

        Changed?.Invoke(this);
        return true;
    }

    /// Channeled: called when the key is released. Toggle: called on the second press.
    public void Release(SkillContext ctx)
    {
        if (!IsRunning) return;

        IsRunning = false;
        ctx.Values = Values;
        _behaviour?.OnDeactivate(ctx);
        ctx.Player.OnSkillEnd();

        Changed?.Invoke(this);
    }

    public void Tick(SkillContext ctx, float deltaTime)
    {
        if (IsEmpty) return;

        bool changed = false;

        if (CooldownRemaining > 0f)
        {
            CooldownRemaining -= deltaTime;
            if (CooldownRemaining <= 0f)
            {
                // WHY: one charge recovers per cooldown period, and the timer restarts while any
                // charge is still missing. A single-charge skill behaves exactly like the old
                // boolean cooldown, so charges are not a separate code path.
                if (ChargesLeft < Values.MaxCharges)
                {
                    ChargesLeft++;
                    if (ChargesLeft < Values.MaxCharges) CooldownRemaining = Values.Cooldown;
                }
                changed = true;
            }
        }

        if (IsRunning)
        {
            ctx.Values = Values;
            _behaviour?.OnTick(ctx, deltaTime);

            if (_behaviour != null && _behaviour.WantsToStop)
            {
                Release(ctx);
                return;
            }

            if (Values.CostPerTick > 0 && Time.time >= _nextChannelTickAt)
            {
                _nextChannelTickAt = Time.time + Mathf.Max(0.05f, Definition.tickInterval);

                if (!ctx.Player.ConsumeMana(Values.CostPerTick))
                {
                    // out of mana ends the channel — the behaviour never has to check for this
                    Release(ctx);
                    return;
                }
                changed = true;
            }
        }

        if (changed) Changed?.Invoke(this);
    }
}
