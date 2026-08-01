using System.Collections.Generic;
using UnityEngine;

public enum SkillActivation
{
    /// Press: fires once.
    Instant,
    /// Hold: runs while held, pays a cost per tick.
    Channeled,
    /// Press on, press off.
    Toggle,
    /// No key. Works from the moment it sits in a slot.
    Passive,
}

public enum SkillRole
{
    Basic,
    Sub,
    EX,
}

public enum ParamType
{
    Float,
    Int,
    Percent,
    Seconds,
}

/// <summary>One tunable number a skill exposes. The config window builds its form from these.</summary>
// WHY: if the editor window hardcoded a form (cooldown / mana / damage), every skill with a number
// of its own — meteor count, fragment count, carpet length — would need the window edited. Skills
// declaring their own parameters means the window never changes and a skill written later still
// gets sliders, ranges and hover help.
[System.Serializable]
public struct SkillParameter
{
    [Tooltip("Stable key. Overrides reference it by this — renaming orphans them.")]
    public string id;
    public string label;
    [Tooltip("What this affects, and in what unit. Shown as hover help.")]
    public string tooltip;
    public ParamType type;
    public float defaultValue;
    public float min;
    public float max;
}

[CreateAssetMenu(fileName = "Skill", menuName = "ScriptableObjects/Skill")]
public class SkillDefinitionSO : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Stable key used by save data and by per-tank overrides. Never rename.")]
    public string skillId;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Role")]
    [Tooltip("Which of the three slots this skill may occupy.")]
    public SkillRole role = SkillRole.Sub;

    [Header("Activation")]
    public SkillActivation activation = SkillActivation.Instant;
    [Tooltip("Seconds to recover one charge. Ignored by Passive.")]
    public float cooldown = 5f;
    [Tooltip("Uses available before waiting. 1 for an ordinary skill.")]
    public int maxCharges = 1;
    public int manaCostOnActivate = 10;
    [Tooltip("Channeled / Toggle only.")]
    public int manaCostPerTick;
    [Tooltip("Channeled / Toggle only.")]
    public float tickInterval = 0.25f;

    [Header("Effect")]
    [Tooltip("Prefab carrying one ISkillBehaviour component. Instantiated once, when equipped.")]
    public GameObject behaviourPrefab;

    [Header("Tuning")]
    public List<SkillParameter> parameters = new List<SkillParameter>();

    [Header("Scaling — which global stats reach this skill")]
    public bool scalesWithSpDamage = true;
    public bool scalesWithSpArea = true;
    public bool scalesWithSpCost = true;

    public bool TryGetParameter(string id, out SkillParameter parameter)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            if (parameters[i].id == id)
            {
                parameter = parameters[i];
                return true;
            }
        }

        parameter = default;
        return false;
    }
}
