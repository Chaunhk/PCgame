using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
    [SerializeField] private float baseSize;
    [SerializeField] private GameObject _firePrefab;
    [SerializeField] private SkillIconControl skillIcon;
    [SerializeField] private SkillBar skillBar;
    //private int _usageCost;
    
    //public void UpdateFire(Vector2 mousePosition){}
    public void EnableFire(Transform bTransform){
        if(playerManager.ManaCheck(_aciveCost)&&!skillIcon.CheckCoolDown(skillBar)){
            skillIcon.SkillCooldown(skillBar);
            transform.position = bTransform.position;
            transform.rotation = bTransform.rotation;
            //fire.transform.localScale *=manager.playerStat.spMod;
            playerManager.ConsumeMana(_aciveCost);
            gameObject.SetActive(true);
            playerManager.OnSkillEnd();
            //StartCoroutine(ManaDelay());
        }
    }
    // public void DisableFire(){
        
    //     playerManager.OnSkillEnd();
        
    // }
    
    // IEnumerator ManaDelay()
    // {
    //     _isCostDelay = true;
    //     yield return new WaitForSeconds(_costDelay);
    //     _isCostDelay = false;
    //     //playerManager.OnSkillEnd();
        
    // }
    // WHY: this used to light TWO fires per shell — one taken from the shared array, and then
    // the serialized `fire` object again on the two lines below the loop. The second one also
    // ignored the size modifier, so a leftover unscaled fire sat under every scaled one.
    protected override void OnHit()
    {
        float scale = manager.playerStat.spMod * baseSize;

        PooledProjectile spawned = manager.projectilePool.Spawn(_firePrefab, transform.position, Quaternion.identity);
        if (spawned == null) return;   // pool logs the reason; nothing sensible to do here

        spawned.transform.localScale = new Vector3(scale, scale, 1);
    }
}
