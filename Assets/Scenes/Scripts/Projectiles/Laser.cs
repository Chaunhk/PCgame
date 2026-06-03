using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Laser : GeneralProjectile
{
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private GameObject _hitbox;
    [SerializeField] private bool _isActive;
    [SerializeField] private bool _isCostDelay;
    [SerializeField] private float _costDelay;
    private int _aciveCost;
    private int _usageCost;
    protected override void Start()
    {
        base.Start();
        _aciveCost = (int)manager.playerStat.spCost;
        _usageCost = _aciveCost/2;
    }
    public void UpdateLaser(Vector2 mousePosition)
    {
        Vector3 shootPoint = new Vector3(manager.shootPoint.transform.position.x, manager.shootPoint.transform.position.y, 0);

        Vector2 dir = mousePosition - (Vector2)shootPoint;
        RaycastHit2D hit = Physics2D.Raycast(shootPoint, dir.normalized, 50, _layerMask);

        // ✅ Extend to default distance if nothing is hit
        float defaultDistance = 23f;
        Vector2 endPoint = hit ? hit.point : (Vector2)shootPoint + dir.normalized * defaultDistance;
        Vector3 endPoint3D = new Vector3(endPoint.x, endPoint.y, 0);

        _lineRenderer.SetPosition(0, shootPoint);
        _lineRenderer.SetPosition(1, endPoint3D);

        Transform boxTransform = _hitbox.transform;
        float distance = Vector3.Distance(shootPoint, endPoint3D);
        float avgx = (shootPoint.x + endPoint.x) / 2;
        float avgy = (shootPoint.y + endPoint.y) / 2;
        float rotZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        boxTransform.localScale = new Vector3(distance, boxTransform.localScale.y, 1);
        boxTransform.rotation = Quaternion.Euler(0, 0, rotZ);
        boxTransform.position = new Vector3(avgx, avgy, 0);

        if (!_isCostDelay)
        {
            if (playerManager.ConsumeMana(_usageCost))
                StartCoroutine(ManaDelay());
            else DisableLaser();
        }
    }
    public void EnableLaser(){
        if(playerManager.ManaCheck(_aciveCost)){
            playerManager.ConsumeMana(_aciveCost);
            _lineRenderer.enabled = true;
            _hitbox.SetActive(true);
            playerManager.OnSkillStart();
            StartCoroutine(ManaDelay());
        }
    }
    public void DisableLaser(){
        _lineRenderer.enabled = false;
        _hitbox.SetActive(false);
        playerManager.OnSkillEnd();
    }
    
    IEnumerator ManaDelay()
    {
        _isCostDelay = true;
        yield return new WaitForSeconds(_costDelay);
        _isCostDelay = false;
        
    }
}
