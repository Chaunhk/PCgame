using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossMovement : MonoBehaviour
{
    // ── States ────────────────────────────────────────────────────────────────
    public enum MoveState { Idle, Chase, CircleStrafe, Charge, Retreat }
 
    [Header("State")]
    public MoveState currentState = MoveState.Chase;
 
    // ── Tuning ────────────────────────────────────────────────────────────────
    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float chaseStopRadius = 2f;   // stops chasing inside this range
 
    [Header("Circle Strafe")]
    [SerializeField] private float strafeSpeed = 2.5f;
    [SerializeField] private float strafeRadius = 4f;      // desired orbit distance from player
    [SerializeField] private float strafeRadiusTolerance = 0.5f;
    [SerializeField] private float strafeCorrectSpeed = 2f; // how fast it corrects its orbit distance
    [SerializeField] private float strafeDuration = 3f;
    [SerializeField] private bool strafeClockwise = false;
 
    [Header("Charge")]
    [SerializeField] private float chargeSpeed = 10f;
    [SerializeField] private float chargeWindupTime = 0.6f; // pause before lunging
    [SerializeField] private float chargeDuration = 0.4f;   // how long the lunge lasts
    [SerializeField] private float chargeCooldown = 2f;
    private bool isCharging = false;
    private bool chargeOnCooldown = false;
    private Vector2 chargeDir;
 
    [Header("Retreat")]
    [SerializeField] private float retreatSpeed = 4f;
    [SerializeField] private float retreatDistance = 5f;   // flees until this far from player
    [SerializeField] private float retreatDuration = 1.5f;
 
    // ── Internal ──────────────────────────────────────────────────────────────
    private Rigidbody2D rb;
    private Transform player;
    private GameManager manager;
 
    private float stateTimer = 0f;
 
    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        manager = GameManager.Instance;
        player = manager.player.transform;
    }
 
    private void Update()
    {
        if (player == null || isCharging) return;
 
        stateTimer -= Time.deltaTime;
    }
 
    private void FixedUpdate()
    {
        if (player == null || isCharging) return;
 
        switch (currentState)
        {
            case MoveState.Idle:
                rb.velocity = Vector2.zero;
                break;
 
            case MoveState.Chase:
                TickChase();
                break;
 
            case MoveState.CircleStrafe:
                TickCircleStrafe();
                break;
 
            case MoveState.Charge:
                StartChargeSequence();
                break;
 
            case MoveState.Retreat:
                TickRetreat();
                break;
        }
    }
 
    // ── Chase ─────────────────────────────────────────────────────────────────
    private void TickChase()
    {
        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= chaseStopRadius)
        {
            rb.velocity = Vector2.zero;
            return;
        }
 
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        rb.velocity = dir * chaseSpeed;
    }
 
    // ── Circle Strafe ─────────────────────────────────────────────────────────
    // Orbits the player at strafeRadius, correcting inward/outward drift each frame.
    private void TickCircleStrafe()
    {
        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        float dist = toPlayer.magnitude;
 
        // Tangent direction (perpendicular to the player-boss axis)
        Vector2 tangent = strafeClockwise
            ? new Vector2(toPlayer.y, -toPlayer.x).normalized
            : new Vector2(-toPlayer.y, toPlayer.x).normalized;
 
        // Radial correction: push toward the desired orbit radius
        float radialError = dist - strafeRadius;
        Vector2 radialCorrection = toPlayer.normalized * (radialError * strafeCorrectSpeed);
 
        rb.velocity = tangent * strafeSpeed + radialCorrection;
 
        // End strafe when timer expires
        if (stateTimer <= 0f)
            SetState(MoveState.Chase);
    }
 
    // ── Charge ────────────────────────────────────────────────────────────────
    // Three-step: windup pause → lock direction → lunge → cooldown.
    private void StartChargeSequence()
    {
        if (chargeOnCooldown) { SetState(MoveState.Chase); return; }
        StartCoroutine(ChargeRoutine());
    }
 
    private IEnumerator ChargeRoutine()
    {
        isCharging = true;
        chargeOnCooldown = true;
 
        // Windup — stand still, face player
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(chargeWindupTime);
 
        // Lock direction at the moment of launch
        chargeDir = ((Vector2)player.position - (Vector2)transform.position).normalized;
 
        // Lunge
        float elapsed = 0f;
        while (elapsed < chargeDuration)
        {
            rb.velocity = chargeDir * chargeSpeed;
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
 
        rb.velocity = Vector2.zero;
        isCharging = false;
 
        // Transition out — retreat after a charge feels natural
        SetState(MoveState.Retreat);
 
        yield return new WaitForSeconds(chargeCooldown);
        chargeOnCooldown = false;
    }
 
    // ── Retreat ───────────────────────────────────────────────────────────────
    private void TickRetreat()
    {
        float dist = Vector2.Distance(transform.position, player.position);
 
        if (dist >= retreatDistance || stateTimer <= 0f)
        {
            SetState(MoveState.Chase);
            return;
        }
 
        Vector2 awayFromPlayer = ((Vector2)transform.position - (Vector2)player.position).normalized;
        rb.velocity = awayFromPlayer * retreatSpeed;
    }
 
    // ── Public API ────────────────────────────────────────────────────────────
 
    /// <summary>
    /// Transition to a new movement state. Call this from your BossController
    /// when entering a new phase or triggering a specific attack.
    /// </summary>
    public void SetState(MoveState newState)
    {
        currentState = newState;
 
        switch (newState)
        {
            case MoveState.CircleStrafe:
                stateTimer = strafeDuration;
                strafeClockwise = Random.value > 0.5f; // randomise orbit direction each time
                break;
 
            case MoveState.Retreat:
                stateTimer = retreatDuration;
                break;
        }
    }
 
    /// <summary>
    /// Convenience wrappers for common boss controller calls.
    /// </summary>
    public void StartChase()   => SetState(MoveState.Chase);
    public void StartStrafe()  => SetState(MoveState.CircleStrafe);
    public void StartCharge()  => SetState(MoveState.Charge);
    public void StartRetreat() => SetState(MoveState.Retreat);
    public void StopMovement() => SetState(MoveState.Idle);
}
