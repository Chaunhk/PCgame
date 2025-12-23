using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    private PlayerStatSO _playerStat;
    public float moveSpeed;
    public float bodyRotatingSpeed;
    public float headRotatingSpeed;
    private void Start()
    {
        InitMovement();
    }
    private void InitMovement()
    {
        _playerStat = GameManager.Instance.playerStat;
        moveSpeed = _playerStat.moveSpeed;
        bodyRotatingSpeed = _playerStat.bodyRotatingSpeed;
        headRotatingSpeed = _playerStat.headRotatingSpeed;
    }
    public void Update()
    {
        PlayerMove();
    }
    private void PlayerMove()
{
    float moveX = Input.GetAxis("Horizontal");
    float moveY = Input.GetAxis("Vertical");

    Vector3 movement = new Vector3(moveX, moveY, 0f);

    // Move
    if (movement.sqrMagnitude > 0.01f)
    {
        movement = movement.normalized;
        transform.position += moveSpeed * Time.deltaTime * movement;

        // Calculate target rotation
        float angle = Mathf.Atan2(movement.y, movement.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);

        // Smooth delayed rotation
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            bodyRotatingSpeed * Time.deltaTime
        );
    }
}
}
