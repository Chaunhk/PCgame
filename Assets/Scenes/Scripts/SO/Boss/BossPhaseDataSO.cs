using UnityEngine;

/// <summary>
/// One phase of a boss fight. Create via Assets > Create > Boss > Phase Data.
/// Phases are evaluated highest-to-lowest healthPercent, so put Phase 1 at 1.0,
/// Phase 2 at 0.6, Phase 3 at 0.3, etc.
/// </summary>
[CreateAssetMenu(fileName = "BossPhaseData", menuName = "Boss/Phase Data")]
public class BossPhaseDataSO : ScriptableObject
{
    [Header("Trigger")]
    [Tooltip("Boss enters this phase when HP drops below this fraction (0–1).")]
    [Range(0f, 1f)] public float healthThreshold = 1f;

    [Header("Movement")]
    public BossMovement.MoveState moveState = BossMovement.MoveState.Chase;

    [Header("Attack")]
    [Tooltip("Seconds between each attack attempt in this phase.")]
    public float attackInterval = 2f;

    [Tooltip("How long the boss pauses movement before attacking.")]
    public float attackWindupTime = 0.3f;
}