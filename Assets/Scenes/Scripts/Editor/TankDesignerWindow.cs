using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools -> Tank Designer. Authors tanks, their loadouts and their numbers without opening code or
/// hand-making assets.
/// </summary>
// WHY: this window renders a form generated from each skill's declared parameters rather than a
// hardcoded one. A skill added later — with a meteor count, a fragment count, whatever — gets a
// full editing form with sliders, ranges and hover help without this file changing.
//
// It lives in an Editor folder rather than carrying an assembly definition: the runtime scripts have
// no asmdef, so they compile into the predefined Assembly-CSharp, and an asmdef assembly cannot
// reference a predefined one. The Editor folder achieves the same fence — none of this ships in a
// player build.
public class TankDesignerWindow : EditorWindow
{
    private const string SkillFolder = "Assets/Scenes/Scripts/SO/Skills";
    private const string TankFolder = "Assets/Scenes/Scripts/SO/Tanks";

    private TankRosterSO _roster;
    private TankDefinitionSO _selected;
    private SkillRole _inspectedRole = SkillRole.Basic;

    private Vector2 _listScroll, _detailScroll, _paramScroll;
    private List<SkillDefinitionSO> _allSkills = new List<SkillDefinitionSO>();

    private GUIStyle _overriddenStyle;
    private GUIStyle _headerStyle;

    [MenuItem("Tools/Tank Designer")]
    public static void Open()
    {
        TankDesignerWindow window = GetWindow<TankDesignerWindow>("Tank Designer");
        window.minSize = new Vector2(900f, 460f);
        window.Reload();
    }

    private void OnFocus()
    {
        Reload();
    }

    private void Reload()
    {
        _allSkills.Clear();
        foreach (string guid in AssetDatabase.FindAssets("t:SkillDefinitionSO"))
        {
            var skill = AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (skill != null) _allSkills.Add(skill);
        }

        if (_roster == null)
        {
            string[] rosters = AssetDatabase.FindAssets("t:TankRosterSO");
            if (rosters.Length > 0)
                _roster = AssetDatabase.LoadAssetAtPath<TankRosterSO>(AssetDatabase.GUIDToAssetPath(rosters[0]));
        }

        if (_selected == null && _roster != null) _selected = _roster.Default;
    }

    private void BuildStyles()
    {
        if (_overriddenStyle != null) return;

        _overriddenStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
        _headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
    }

    private void OnGUI()
    {
        BuildStyles();

        EditorGUILayout.BeginHorizontal();
        DrawTankList();
        DrawTankDetail();
        DrawParameterForm();
        EditorGUILayout.EndHorizontal();
    }

    // ---------------------------------------------------------------- left: the roster

    private void DrawTankList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(210f));
        EditorGUILayout.LabelField("TANKS", _headerStyle);

        EditorGUI.BeginChangeCheck();
        _roster = (TankRosterSO)EditorGUILayout.ObjectField(_roster, typeof(TankRosterSO), false);
        if (EditorGUI.EndChangeCheck()) _selected = _roster != null ? _roster.Default : null;

        if (_roster == null)
        {
            EditorGUILayout.HelpBox("No tank roster. Create one to list the tanks a player can pick.", MessageType.Info);
            if (GUILayout.Button("Create roster")) CreateRoster();
            EditorGUILayout.EndVertical();
            return;
        }

        _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
        for (int i = 0; i < _roster.tanks.Count; i++)
        {
            TankDefinitionSO tank = _roster.tanks[i];
            if (tank == null) continue;

            bool isDefault = tank == _roster.Default;
            string label = tank.displayName + (isDefault ? "  (default)" : string.Empty);

            GUI.backgroundColor = tank == _selected ? new Color(0.6f, 0.8f, 1f) : Color.white;
            if (GUILayout.Button(label, EditorStyles.miniButton)) _selected = tank;
            GUI.backgroundColor = Color.white;
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        if (GUILayout.Button("+ New tank")) CreateTank(null);
        using (new EditorGUI.DisabledScope(_selected == null))
        {
            if (GUILayout.Button("Duplicate selected")) CreateTank(_selected);
            if (GUILayout.Button("Make default") && _selected != null)
            {
                Undo.RecordObject(_roster, "Set default tank");
                _roster.defaultTankId = _selected.tankId;
                EditorUtility.SetDirty(_roster);
            }
        }

        EditorGUILayout.EndVertical();
    }

    // ---------------------------------------------------------------- middle: identity + loadout

    private void DrawTankDetail()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(320f));

        if (_selected == null)
        {
            EditorGUILayout.LabelField("Select a tank on the left.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
            return;
        }

        _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
        EditorGUILayout.LabelField(_selected.displayName.ToUpperInvariant(), _headerStyle);

        Undo.RecordObject(_selected, "Edit tank");

        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        _selected.displayName = EditorGUILayout.TextField("Name", _selected.displayName);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent("Id", "Stable key used by save data. Renaming orphans a player's saved choice."), GUILayout.Width(EditorGUIUtility.labelWidth));
        EditorGUILayout.SelectableLabel(_selected.tankId, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        EditorGUILayout.EndHorizontal();
        _selected.portrait = (Sprite)EditorGUILayout.ObjectField("Portrait", _selected.portrait, typeof(Sprite), false);
        _selected.bodyPrefab = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Body prefab", "This tank's own prefab. Must carry a TankRig on its root."),
            _selected.bodyPrefab, typeof(GameObject), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Base stats", EditorStyles.boldLabel);
        _selected.baseStats = (PlayerStatSO)EditorGUILayout.ObjectField("Stat asset", _selected.baseStats, typeof(PlayerStatSO), false);
        if (_selected.baseStats != null) DrawBaseStats(_selected.baseStats);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Loadout", EditorStyles.boldLabel);
        DrawSlot("Basic", SkillRole.Basic, _selected.basicSlot);
        DrawSlot("Sub", SkillRole.Sub, _selected.subSlot);
        DrawSlot("EX", SkillRole.EX, _selected.exSlot);

        EditorGUILayout.Space();
        DrawValidation();

        EditorGUILayout.EndScrollView();

        if (GUI.changed) EditorUtility.SetDirty(_selected);

        EditorGUILayout.EndVertical();
    }

    private void DrawBaseStats(PlayerStatSO stats)
    {
        Undo.RecordObject(stats, "Edit base stats");
        EditorGUI.indentLevel++;
        stats.maxHealth = EditorGUILayout.IntField("Max health", stats.maxHealth);
        stats.maxMana = EditorGUILayout.IntField("Max mana", stats.maxMana);
        stats.manaRegen = EditorGUILayout.IntField("Mana regen", stats.manaRegen);
        stats.healthRegen = EditorGUILayout.IntField("Health regen", stats.healthRegen);
        stats.moveSpeed = EditorGUILayout.FloatField("Move speed", stats.moveSpeed);
        stats.damage = EditorGUILayout.IntField("Bullet damage", stats.damage);
        EditorGUI.indentLevel--;
        if (GUI.changed) EditorUtility.SetDirty(stats);
    }

    private void DrawSlot(string label, SkillRole role, SkillBinding binding)
    {
        EditorGUILayout.BeginHorizontal();

        // WHY: the dropdown lists only skills of this role, so an invalid loadout is not selectable
        // and therefore needs no error message.
        List<SkillDefinitionSO> candidates = new List<SkillDefinitionSO>();
        List<string> names = new List<string> { "(empty)" };
        int current = 0;

        foreach (SkillDefinitionSO skill in _allSkills)
        {
            if (skill.role != role) continue;
            candidates.Add(skill);
            names.Add(skill.displayName);
            if (skill == binding.skill) current = candidates.Count;
        }

        int picked = EditorGUILayout.Popup(label, current, names.ToArray());
        if (picked != current)
        {
            binding.skill = picked == 0 ? null : candidates[picked - 1];
            binding.overrides.Clear();   // overrides belong to the skill that was there
            EditorUtility.SetDirty(_selected);
        }

        using (new EditorGUI.DisabledScope(binding.skill == null))
        {
            if (GUILayout.Button("Tune", GUILayout.Width(50f))) _inspectedRole = role;
        }

        EditorGUILayout.EndHorizontal();

        if (binding.skill != null && binding.overrides.Count > 0)
            EditorGUILayout.LabelField(" ", $"{binding.overrides.Count} value(s) overridden", EditorStyles.miniLabel);
    }

    // ---------------------------------------------------------------- right: generated form

    private void DrawParameterForm()
    {
        EditorGUILayout.BeginVertical();

        SkillBinding binding = _selected != null ? _selected.GetBinding(_inspectedRole) : null;
        if (binding == null || binding.skill == null)
        {
            EditorGUILayout.LabelField("Pick a skill and press Tune.", EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndVertical();
            return;
        }

        SkillDefinitionSO skill = binding.skill;
        EditorGUILayout.LabelField($"{skill.displayName.ToUpperInvariant()}   ({_inspectedRole})", _headerStyle);
        EditorGUILayout.LabelField(skill.description, EditorStyles.wordWrappedMiniLabel);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shared by every tank", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        Undo.RecordObject(skill, "Edit skill");
        skill.activation = (SkillActivation)EditorGUILayout.EnumPopup(new GUIContent("Activation", "Instant fires once; Channeled runs while held; Toggle is on/off; Passive has no key."), skill.activation);
        skill.cooldown = EditorGUILayout.FloatField(new GUIContent("Cooldown", "Seconds to recover one charge."), skill.cooldown);
        skill.maxCharges = EditorGUILayout.IntField(new GUIContent("Charges", "Uses available before waiting. 1 for an ordinary skill."), skill.maxCharges);
        skill.manaCostOnActivate = EditorGUILayout.IntField("Mana cost", skill.manaCostOnActivate);
        if (GUI.changed) EditorUtility.SetDirty(skill);
        EditorGUI.indentLevel--;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Tuned for {_selected.displayName}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Bold = overridden for this tank. Revert makes it follow the skill again.", EditorStyles.miniLabel);

        _paramScroll = EditorGUILayout.BeginScrollView(_paramScroll);

        if (skill.parameters.Count == 0)
            EditorGUILayout.HelpBox("This skill declares no tunable parameters yet.", MessageType.Info);

        foreach (SkillParameter parameter in skill.parameters)
            DrawParameterRow(binding, parameter);

        DrawStaleOverrides(binding, skill);

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(binding.overrides.Count == 0))
        {
            if (GUILayout.Button("Revert all to skill defaults"))
            {
                Undo.RecordObject(_selected, "Revert overrides");
                binding.overrides.Clear();
                EditorUtility.SetDirty(_selected);
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawParameterRow(SkillBinding binding, SkillParameter parameter)
    {
        int index = binding.overrides.FindIndex(o => o.parameterId == parameter.id);
        bool overridden = index >= 0;
        float value = overridden ? binding.overrides[index].value : parameter.defaultValue;

        EditorGUILayout.BeginHorizontal();

        var content = new GUIContent(
            parameter.label + (overridden ? " *" : string.Empty),
            $"{parameter.tooltip}\n\nSkill default: {parameter.defaultValue}");

        EditorGUILayout.LabelField(content, overridden ? _overriddenStyle : EditorStyles.label, GUILayout.Width(190f));

        float edited;
        switch (parameter.type)
        {
            case ParamType.Int:
                edited = EditorGUILayout.IntSlider((int)value, (int)parameter.min, (int)parameter.max);
                break;
            case ParamType.Percent:
                edited = EditorGUILayout.Slider(value * 100f, parameter.min * 100f, parameter.max * 100f) / 100f;
                break;
            default:
                edited = EditorGUILayout.Slider(value, parameter.min, parameter.max);
                break;
        }

        using (new EditorGUI.DisabledScope(!overridden))
        {
            // WHY: revert DELETES the override rather than writing the default back. Writing it back
            // would look identical today and silently stop tracking the skill asset tomorrow.
            if (GUILayout.Button("Revert", GUILayout.Width(56f)) && overridden)
            {
                Undo.RecordObject(_selected, "Revert override");
                binding.overrides.RemoveAt(index);
                EditorUtility.SetDirty(_selected);
                EditorGUILayout.EndHorizontal();
                return;
            }
        }

        EditorGUILayout.EndHorizontal();

        if (Mathf.Approximately(edited, value)) return;

        Undo.RecordObject(_selected, "Set override");

        if (Mathf.Approximately(edited, parameter.defaultValue))
        {
            // back at the default: drop the override so the value keeps following the skill
            if (overridden) binding.overrides.RemoveAt(index);
        }
        else if (overridden)
        {
            binding.overrides[index] = new ParameterOverride { parameterId = parameter.id, value = edited };
        }
        else
        {
            binding.overrides.Add(new ParameterOverride { parameterId = parameter.id, value = edited });
        }

        // stable ordering, so a one-number change stays a one-line diff and two designers do not
        // conflict over list order
        binding.overrides.Sort((a, b) => string.CompareOrdinal(a.parameterId, b.parameterId));
        EditorUtility.SetDirty(_selected);
    }

    private void DrawStaleOverrides(SkillBinding binding, SkillDefinitionSO skill)
    {
        for (int i = binding.overrides.Count - 1; i >= 0; i--)
        {
            ParameterOverride entry = binding.overrides[i];
            if (skill.TryGetParameter(entry.parameterId, out _)) continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox($"'{entry.parameterId}' is not a parameter of this skill any more. It does nothing.", MessageType.Warning);
            if (GUILayout.Button("Delete", GUILayout.Width(56f), GUILayout.Height(38f)))
            {
                Undo.RecordObject(_selected, "Delete stale override");
                binding.overrides.RemoveAt(i);
                EditorUtility.SetDirty(_selected);
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    // ---------------------------------------------------------------- validation

    private void DrawValidation()
    {
        List<string> problems = new List<string>();

        if (string.IsNullOrEmpty(_selected.tankId)) problems.Add("Tank id is empty — save data cannot reference this tank.");
        if (_selected.baseStats == null) problems.Add("No base stat asset. Skill numbers have no baseline to measure against.");
        if (_selected.bodyPrefab == null) problems.Add("No body prefab — this tank cannot be spawned.");
        else if (_selected.bodyPrefab.GetComponent<TankRig>() == null) problems.Add($"'{_selected.bodyPrefab.name}' has no TankRig on its root, so the spawner cannot connect it to the HUD or the camera.");
        if (_selected.basicSlot.skill == null) problems.Add("No basic attack. Skills that inherit from it will contribute nothing.");

        if (_roster != null)
        {
            int sameId = 0;
            foreach (TankDefinitionSO tank in _roster.tanks)
                if (tank != null && tank.tankId == _selected.tankId) sameId++;
            if (sameId > 1) problems.Add($"Another tank in the roster uses the id '{_selected.tankId}'.");
            if (!_roster.tanks.Contains(_selected)) problems.Add("This tank is not in the roster, so the player cannot pick it.");
        }

        if (problems.Count == 0)
        {
            EditorGUILayout.HelpBox("No problems.", MessageType.None);
            return;
        }

        foreach (string problem in problems)
            EditorGUILayout.HelpBox(problem, MessageType.Warning);
    }

    // ---------------------------------------------------------------- asset creation

    private void CreateRoster()
    {
        EnsureFolder(TankFolder);

        var roster = CreateInstance<TankRosterSO>();
        foreach (string guid in AssetDatabase.FindAssets("t:TankDefinitionSO"))
        {
            var tank = AssetDatabase.LoadAssetAtPath<TankDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (tank != null) roster.tanks.Add(tank);
        }

        AssetDatabase.CreateAsset(roster, TankFolder + "/TankRoster.asset");
        AssetDatabase.SaveAssets();

        _roster = roster;
        _selected = roster.Default;
    }

    // WHY: creating assets from here rather than by hand means the file name always matches the id
    // and the asset always lands in the right folder. Hand-made ScriptableObjects are how a project
    // ends up with two files called Data.asset.
    private void CreateTank(TankDefinitionSO copyFrom)
    {
        EnsureFolder(TankFolder);

        var tank = CreateInstance<TankDefinitionSO>();
        string id = copyFrom != null ? copyFrom.tankId + "_copy" : "tank_" + (_roster != null ? _roster.tanks.Count + 1 : 1);
        tank.tankId = id;
        tank.displayName = copyFrom != null ? copyFrom.displayName + " copy" : "New Tank";

        if (copyFrom != null)
        {
            tank.baseStats = copyFrom.baseStats;
            tank.portrait = copyFrom.portrait;
            tank.basicSlot = CloneBinding(copyFrom.basicSlot);
            tank.subSlot = CloneBinding(copyFrom.subSlot);
            tank.exSlot = CloneBinding(copyFrom.exSlot);
        }

        AssetDatabase.CreateAsset(tank, AssetDatabase.GenerateUniqueAssetPath(TankFolder + "/" + id + ".asset"));

        if (_roster != null)
        {
            _roster.tanks.Add(tank);
            EditorUtility.SetDirty(_roster);
        }

        AssetDatabase.SaveAssets();
        _selected = tank;
    }

    private static SkillBinding CloneBinding(SkillBinding source)
    {
        var clone = new SkillBinding { skill = source.skill };
        clone.overrides.AddRange(source.overrides);
        return clone;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        int lastSlash = path.LastIndexOf('/');
        string parent = path.Substring(0, lastSlash);
        string leaf = path.Substring(lastSlash + 1);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
