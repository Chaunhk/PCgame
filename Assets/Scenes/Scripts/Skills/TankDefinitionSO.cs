using System.Collections.Generic;
using UnityEngine;

/// <summary>One value on one skill, changed for one tank.</summary>
// WHY: sparse on purpose. A binding stores only the fields it actually changes, so everything else
// keeps inheriting the skill asset — including edits made to it later. An override that merely
// restates the default is worse than no override: it silently stops tracking the source.
[System.Serializable]
public struct ParameterOverride
{
    public string parameterId;
    public float value;
}

/// <summary>A skill placed in one of a tank's slots, plus that tank's changes to its numbers.</summary>
[System.Serializable]
public class SkillBinding
{
    public SkillDefinitionSO skill;
    public List<ParameterOverride> overrides = new List<ParameterOverride>();
}

[CreateAssetMenu(fileName = "Tank", menuName = "ScriptableObjects/Tank")]
public class TankDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable key used by save data. Never rename.")]
    public string tankId;
    public string displayName;
    public Sprite portrait;

    [Header("Base stats")]
    public PlayerStatSO baseStats;

    [Header("Loadout")]
    // WHY: three named slots rather than an array of three, so an invalid loadout cannot be
    // represented — an EX skill cannot land in the basic slot, and no tank can ship without a basic
    // attack, which is what the "inherits from the basic attack" chain in the fire kit assumes.
    public SkillBinding basicSlot = new SkillBinding();
    public SkillBinding subSlot = new SkillBinding();
    public SkillBinding exSlot = new SkillBinding();

    public SkillBinding GetBinding(SkillRole role)
    {
        switch (role)
        {
            case SkillRole.Basic: return basicSlot;
            case SkillRole.Sub: return subSlot;
            case SkillRole.EX: return exSlot;
            default: return null;
        }
    }
}
