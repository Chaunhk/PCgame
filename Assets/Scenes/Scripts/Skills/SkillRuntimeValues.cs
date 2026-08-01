using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The numbers a skill actually runs with, after the three layers are resolved.
/// </summary>
// WHY: the same skill on a different tank must be able to carry different numbers. If a skill asset
// owned its numbers outright, two tanks sharing a skill would share its cooldown, and the workaround
// would be duplicating the asset — copies then drift and a fix lands in one of them. So numbers
// resolve as: skill default -> sparse per-tank override -> global run modifiers.
public class SkillRuntimeValues
{
    private readonly Dictionary<string, float> _values = new Dictionary<string, float>();
    private readonly Dictionary<string, GameObject> _objects = new Dictionary<string, GameObject>();

    public SkillDefinitionSO Definition { get; private set; }

    public int Cost { get; private set; }
    public int CostPerTick { get; private set; }
    public float Cooldown { get; private set; }
    public int MaxCharges { get; private set; }
    /// Multiplier applied to any area/size a skill produces.
    public float AreaScale { get; private set; }
    /// Flat bonus added to any damage a skill deals.
    public int BonusDamage { get; private set; }

    // WHY: the global stats contribute their CHANGE from the tank's baseline, not their absolute
    // value. spCost starts at 30 and upgrade cards subtract from it, so reading it as an absolute
    // discount made every skill free; spDamage starts at 1, so reading it as an absolute bonus
    // would quietly add 1 to everything. Deltas keep the skill asset the single source of a
    // skill's real numbers while the upgrade cards keep working unchanged.
    public void Resolve(SkillDefinitionSO definition, SkillBinding binding, PlayerStatSO stats, PlayerStatSO baseStats)
    {
        IList<ParameterOverride> overrides = binding != null ? binding.overrides : null;

        Definition = definition;
        _values.Clear();
        _objects.Clear();

        // prefab choices resolve the same two layers: the skill's own, then this tank's swap
        foreach (SkillObjectParameter parameter in definition.objectParameters)
            _objects[parameter.id] = parameter.defaultValue;

        if (binding != null && binding.objectOverrides != null)
        {
            for (int i = 0; i < binding.objectOverrides.Count; i++)
            {
                ObjectOverride entry = binding.objectOverrides[i];
                if (string.IsNullOrEmpty(entry.parameterId)) continue;

                if (!definition.TryGetObjectParameter(entry.parameterId, out _))
                {
                    Debug.LogWarning($"Skill '{definition.skillId}': prefab override for unknown parameter '{entry.parameterId}' is being ignored.");
                    continue;
                }

                _objects[entry.parameterId] = entry.value;
            }
        }

        // layer 1 — the skill's own declared defaults
        foreach (SkillParameter parameter in definition.parameters)
            _values[parameter.id] = parameter.defaultValue;

        // layer 2 — this tank's overrides, sparse: only the fields it actually changes
        if (overrides != null)
        {
            for (int i = 0; i < overrides.Count; i++)
            {
                ParameterOverride entry = overrides[i];
                if (string.IsNullOrEmpty(entry.parameterId)) continue;

                if (!definition.TryGetParameter(entry.parameterId, out _))
                {
                    // WHY: an override left behind after a parameter was renamed or removed would
                    // otherwise sit there doing nothing, and look like a value that is being applied.
                    Debug.LogWarning($"Skill '{definition.skillId}': override for unknown parameter '{entry.parameterId}' is being ignored.");
                    continue;
                }

                _values[entry.parameterId] = entry.value;
            }
        }

        // layer 3 — the run's global modifiers (upgrade cards feed these)
        Cooldown = definition.cooldown;
        MaxCharges = Mathf.Max(1, definition.maxCharges);
        CostPerTick = definition.manaCostPerTick;

        bool hasRunStats = stats != null && baseStats != null;

        int cost = definition.manaCostOnActivate;
        if (hasRunStats && definition.scalesWithSpCost)
            cost -= baseStats.spCost - stats.spCost;     // cost-reduction upgrades lower spCost
        Cost = Mathf.Max(0, cost);

        if (hasRunStats && definition.scalesWithSpArea && baseStats.spMod > 0f)
            AreaScale = Mathf.Max(0.01f, stats.spMod / baseStats.spMod);
        else
            AreaScale = 1f;

        BonusDamage = hasRunStats && definition.scalesWithSpDamage
            ? stats.spDamage - baseStats.spDamage
            : 0;
    }

    /// <summary>A prefab this skill lets you swap, after the tank's choice is applied.</summary>
    public GameObject GetObject(string parameterId, GameObject fallback = null)
    {
        return _objects.TryGetValue(parameterId, out GameObject value) && value != null ? value : fallback;
    }

    public float Get(string parameterId, float fallback = 0f)
    {
        return _values.TryGetValue(parameterId, out float value) ? value : fallback;
    }

    public int GetInt(string parameterId, int fallback = 0)
    {
        return _values.TryGetValue(parameterId, out float value) ? Mathf.RoundToInt(value) : fallback;
    }

    /// A damage number from this skill, with the run's flat bonus already applied.
    public int Damage(string parameterId)
    {
        return Mathf.Max(0, Mathf.RoundToInt(Get(parameterId)) + BonusDamage);
    }

    /// An area/size number from this skill, with the run's area multiplier already applied.
    public float Area(string parameterId)
    {
        return Get(parameterId) * AreaScale;
    }
}
