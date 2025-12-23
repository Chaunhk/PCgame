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
    public void UpdateLaser(Vector2 mousePosition){
        //Make ray shape
        Vector3 shootPoint = manager.shootPoint.transform.position;
        _lineRenderer.SetPosition(0, shootPoint);
        _lineRenderer.SetPosition(1, mousePosition);
        Vector2 dir = mousePosition - (Vector2)shootPoint;
        RaycastHit2D hit = Physics2D.Raycast(shootPoint, dir.normalized,50,_layerMask);
        if(hit){
            _lineRenderer.SetPosition(1,hit.point);
        }
        //Make hit box shape
        Transform boxTransform = _hitbox.transform;
        float distance = Vector3.Distance(shootPoint,hit.point);
        boxTransform.localScale = new Vector3(distance,boxTransform.localScale.y,1);
        float rotZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        boxTransform.rotation = Quaternion.Euler(0, 0, rotZ);
        float avgx = (shootPoint.x + hit.point.x)/2;
        float avgy = (shootPoint.y + hit.point.y)/2;
        boxTransform.position = new Vector3(avgx,avgy,0);
        //Consume mana every set interval
        if(!_isCostDelay){
            if (playerController.ConsumeMana(_usageCost)) {
                StartCoroutine(ManaDelay());
            }
            else DisableLaser();
        }
    }
    public void EnableLaser(){
        if(playerController.ManaCheck(_aciveCost)){
            playerController.ConsumeMana(_aciveCost);
            _lineRenderer.enabled = true;
            _hitbox.SetActive(true);
            playerController.OnSkillStart();
            StartCoroutine(ManaDelay());
        }
    }
    public void DisableLaser(){
        _lineRenderer.enabled = false;
        _hitbox.SetActive(false);
        playerController.OnSkillEnd();
    }
    
    IEnumerator ManaDelay()
    {
        _isCostDelay = true;
        yield return new WaitForSeconds(_costDelay);
        _isCostDelay = false;
        
    }
}
