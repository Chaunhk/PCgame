using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Draws GameManager normally, except the tank pick becomes a dropdown of the roster.
/// </summary>
// WHY: choosing the tank meant dragging a TankDefinitionSO asset into a slot, which requires knowing
// which assets exist and where they live. The roster already lists every tank a player can pick, so
// the inspector can just offer that list — and a tank that is not in the roster cannot be picked by
// accident, which would spawn something the player could never choose themselves.
[CustomEditor(typeof(GameManager))]
public class GameManagerEditor : Editor
{
    private const string SelectedTankField = "selectedTank";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawTankPicker();

        EditorGUILayout.Space();
        DrawPropertiesExcluding(serializedObject, SelectedTankField, "m_Script");

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawTankPicker()
    {
        var manager = (GameManager)target;
        SerializedProperty selected = serializedObject.FindProperty(SelectedTankField);

        EditorGUILayout.LabelField("Tank for this run", EditorStyles.boldLabel);

        if (manager.tankRoster == null || manager.tankRoster.tanks.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Assign a Tank Roster below to pick from a list. Without one, drop a tank asset in directly.",
                MessageType.Info);
            EditorGUILayout.PropertyField(selected, new GUIContent("Selected tank"));
            return;
        }

        List<TankDefinitionSO> tanks = manager.tankRoster.tanks;
        var labels = new List<string>();
        var values = new List<TankDefinitionSO>();

        TankDefinitionSO fallback = manager.tankRoster.Default;
        labels.Add(fallback != null
            ? $"Player's choice  (now: {fallback.displayName})"
            : "Player's choice");
        values.Add(null);

        int current = 0;
        for (int i = 0; i < tanks.Count; i++)
        {
            if (tanks[i] == null) continue;

            values.Add(tanks[i]);
            labels.Add(tanks[i].displayName);
            if (tanks[i] == selected.objectReferenceValue) current = values.Count - 1;
        }

        // a tank set here but missing from the roster would silently show as "Player's choice"
        if (selected.objectReferenceValue != null && current == 0)
        {
            values.Add((TankDefinitionSO)selected.objectReferenceValue);
            labels.Add($"{((TankDefinitionSO)selected.objectReferenceValue).displayName}  (not in roster)");
            current = values.Count - 1;
        }

        int picked = EditorGUILayout.Popup(
            new GUIContent("Take into the run", "Which tank spawns when you press Play. 'Player's choice' uses what the player selected, falling back to the roster default."),
            current, labels.ToArray());

        if (picked != current) selected.objectReferenceValue = values[picked];

        TankDefinitionSO shown = values[Mathf.Clamp(picked, 0, values.Count - 1)] ?? fallback;
        if (shown != null) DrawSummary(shown);

        if (GUILayout.Button("Open Tank Designer")) TankDesignerWindow.Open();
    }

    private static void DrawSummary(TankDefinitionSO tank)
    {
        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Body", tank.bodyPrefab != null ? tank.bodyPrefab.name : "— none, cannot spawn");
        EditorGUILayout.LabelField("Basic", Describe(tank.basicSlot));
        EditorGUILayout.LabelField("Sub", Describe(tank.subSlot));
        EditorGUILayout.LabelField("EX", Describe(tank.exSlot));
        EditorGUI.indentLevel--;
    }

    private static string Describe(SkillBinding binding)
    {
        if (binding == null || binding.skill == null) return "— empty";

        int overrides = binding.overrides != null ? binding.overrides.Count : 0;
        return overrides > 0
            ? $"{binding.skill.displayName}  ({overrides} tuned)"
            : binding.skill.displayName;
    }
}
