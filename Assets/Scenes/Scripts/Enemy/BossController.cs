using System;
using System.Collections;
using UnityEngine;
 
/// <summary>
/// Main boss class. Attach alongside BossMovement on the boss GameObject.
/// Extends GeneralEnemy so it reuses Damage(), iframes, healthBar, and Dead().
///
/// Setup in Inspector:
///  1. Assign phases[] in order from Phase 1 (healthThreshold = 1.0) down to final phase.
///  2. Subscribe to OnAttack to wire up your projectile / AoE spawner.
///  3. Optionally subscribe to OnPhaseChanged for VFX / audio triggers.
/// </summary>
[RequireComponent(typeof(BossMovement))]
public class BossController : GeneralEnemy
{
    // ── Events ─────────────────────────────────────────────────────────────────
    /// <summary>Fired each time the boss attacks. Payload = current phase index.</summary>
    public event Action<int> OnAttack;
 
    /// <summary>Fired when a new phase begins. Payload = new phase index.</summary>
    public event Action<int> OnPhaseChanged;
 
    /// <summary>Fired once when the boss dies, before DeSpawn.</summary>
    public event Action OnBossDead;
 
    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("Boss Phases")]
    [Tooltip("Define phases highest-threshold first. E.g. [0]=1.0, [1]=0.6, [2]=0.3")]
    [SerializeField] private BossPhaseDataSO[] phases;
 
    [Header("Intro")]
    [Tooltip("Boss stays Idle for this many seconds before the fight starts.")]
    [SerializeField] private float introDelay = 1.5f;
 
    // ── Runtime ────────────────────────────────────────────────────────────────
    private BossMovement movement;
    private int currentPhaseIndex = -1;
    private BossPhaseDataSO CurrentPhase => phases[currentPhaseIndex];
    private bool fightStarted = false;
    private bool isAttacking = false;
 
    // ── Unity Lifecycle ────────────────────────────────────────────────────────
 
    // Called by MobPoolManager (same as base), then we add boss-specific init.
    public override void Initialize(DropService service, UnityEngine.Pool.IObjectPool<GeneralEnemy> poolRef)
    {
        // Reset boss state BEFORE base.Initialize — base calls StopAllCoroutines()
        // internally, so any coroutine we start must come after the base call.
        fightStarted = false;
        isAttacking = false;
        currentPhaseIndex = -1;

        base.Initialize(service, poolRef); // sets manager, levelManager, stats, healthBar

        movement = GetComponent<BossMovement>();
        StartCoroutine(IntroRoutine());
    }
 
    /// <summary>
    /// Suppress GeneralEnemy's built-in movement update entirely.
    /// BossMovement handles all locomotion.
    /// </summary>
    protected override void Update()
    {
        if (!fightStarted) return;
        if (manager == null || manager.player == null) return;
 
        CheckPhaseTransition();
    }
 
    // ── Intro ──────────────────────────────────────────────────────────────────
    private IEnumerator IntroRoutine()
    {
        movement.StopMovement();
        yield return new WaitForSeconds(introDelay);
        fightStarted = true;
        EnterPhase(0);
    }
 
    // ── Phase Logic ────────────────────────────────────────────────────────────
    private void CheckPhaseTransition()
    {
        if (phases == null || phases.Length == 0) return;
 
        float hpPercent = (float)currentHealth / maxHealth;
 
        // Walk forward through phases — skip phases already active or passed
        int nextPhase = currentPhaseIndex + 1;
        if (nextPhase >= phases.Length) return;
 
        if (hpPercent <= phases[nextPhase].healthThreshold)
            EnterPhase(nextPhase);
    }
 
    private void EnterPhase(int index)
    {
        if (index == currentPhaseIndex) return;
 
        currentPhaseIndex = index;
        BossPhaseDataSO phase = phases[currentPhaseIndex];
 
        movement.SetState(phase.moveState);
 
        StopCoroutine(nameof(AttackLoop)); // stop previous loop if running
        StartCoroutine(AttackLoop());
 
        OnPhaseChanged?.Invoke(currentPhaseIndex);
 
        Debug.Log($"[BossController] Entered phase {currentPhaseIndex} " +
                  $"({phase.name}) — {phase.moveState}, interval {phase.attackInterval}s");
    }
 
    // ── Attack Loop ────────────────────────────────────────────────────────────
    /// <summary>
    /// Fires on a timer per phase. Pauses movement briefly (windup), fires the
    /// attack event, then resumes. Replace movement pause with an animation
    /// trigger if you have an animator.
    /// </summary>
    private IEnumerator AttackLoop()
    {
        while (fightStarted && currentPhaseIndex >= 0)
        {
            yield return new WaitForSeconds(CurrentPhase.attackInterval);
 
            if (!fightStarted) yield break;
 
            yield return StartCoroutine(AttackOnce());
        }
    }
 
    private IEnumerator AttackOnce()
    {
        if (isAttacking) yield break;
        isAttacking = true;
 
        // Brief windup — pause movement so the attack has a readable tell
        BossMovement.MoveState previousState = movement.currentState;
        movement.StopMovement();
 
        yield return new WaitForSeconds(CurrentPhase.attackWindupTime);
 
        // Fire — listeners (projectile spawner, AoE, etc.) handle the actual attack
        OnAttack?.Invoke(currentPhaseIndex);
 
        // Resume movement
        movement.SetState(previousState);
        isAttacking = false;
    }
 
    // ── Death Override ─────────────────────────────────────────────────────────
    public override void Dead()
    {
        fightStarted = false;
        StopAllCoroutines();
        movement.StopMovement();
 
        OnBossDead?.Invoke();
 
        base.Dead(); // handles Drop() + DeSpawn()
    }
 
    // ── Public Utilities ───────────────────────────────────────────────────────
 
    /// <summary>
    /// Force a specific phase immediately — useful for cutscenes or debug.
    /// </summary>
    public void ForcePhase(int index)
    {
        if (index < 0 || index >= phases.Length) return;
        EnterPhase(index);
    }
 
    /// <summary>Returns 0–1 HP fraction. Handy for boss health bar UI.</summary>
    public float HealthPercent => (float)currentHealth / maxHealth;
}