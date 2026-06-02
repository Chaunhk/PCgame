using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class Canon : Bullet
{
    //[SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private LayerMask _layerMask;
    //[SerializeField] private GameObject _hitbox;
    [SerializeField] private bool _isActive;
    [SerializeField] private bool _isCostDelay;
    [SerializeField] private float _costDelay;
    [SerializeField] private int _aciveCost;
    [SerializeField] private GameObject fire;
    [SerializeField] private SkillIconControl skillIcon;
    [SerializeField] private SkillBar skillBar;
    private int _usageCost;
    
    //public void UpdateFire(Vector2 mousePosition){}
    public void EnableFire(Transform bTransform){
        if(playerManager.ManaCheck(_aciveCost)&&!skillIcon.CheckCoolDown(skillBar)){
            skillIcon.SkillCooldown(skillBar);
            transform.position = bTransform.position;
            transform.rotation = bTransform.rotation;
            playerManager.ConsumeMana(_aciveCost);
            gameObject.SetActive(true);
            playerManager.OnSkillStart();
            StartCoroutine(ManaDelay());
        }
    }
    public void DisableFire(){
        
        playerManager.OnSkillEnd();
        
    }
    
    IEnumerator ManaDelay()
    {
        _isCostDelay = true;
        yield return new WaitForSeconds(_costDelay);
        _isCostDelay = false;
        
    }
    public override void DisableBullet()
    {
        fire.transform.position = transform.position;
        fire.SetActive(true);
    }
}
