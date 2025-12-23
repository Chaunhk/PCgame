using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : PlayerController
{
    private PlayerStatSO _playerStat;
    public float moveSpeed;
    public float bodyRotatingSpeed;
    public float headRotatingSpeed;
    private void Start()
    {
        manager = GameManager.Instance;
        _playerStat = manager.playerStat;
        InitMovement();
        //gameObject.SetActive(false);
    }
    private void InitMovement()
    {
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
        Vector3 movement = new Vector3(moveX, moveY, 0f).normalized;
        transform.position += movement * moveSpeed * Time.deltaTime;
    }
}
