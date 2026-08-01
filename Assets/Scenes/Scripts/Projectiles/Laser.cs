using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Laser : GeneralProjectile
{
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private GameObject _hitbox;
    [SerializeField] private bool _isActive;
    [SerializeField] private bool _isCostDelay;
    [SerializeField] private float _costDelay;
    [SerializeField] private SkillIconControl skillIcon;
    [SerializeField] private SkillBar skillBar;
    [SerializeField] private Material _laserMaterial;
    private int _aciveCost;
    private int _usageCost;
    protected override void Start()
    {
        base.Start();
        _aciveCost = (int)manager.playerStat.spCost;
        _usageCost = _aciveCost/2;
        SetupLaserMaterial();

    }
    // WHY: this sourced its material from Shader.Find("Sprites/Default"), a by-name lookup that
    // returns null in a player build unless something else already references that shader — the
    // laser would then render magenta in a build while looking correct in the editor. Assign
    // _laserMaterial in the inspector to bind a real asset; the lookup stays as a fallback so
    // nothing breaks before the field is wired up.
    private void SetupLaserMaterial()
    {
        Material laserMat = _laserMaterial != null
            ? new Material(_laserMaterial)
            : new Material(Shader.Find("Sprites/Default"));
        laserMat.color = new Color(1f, 0.3f, 0f, 0.8f); // orange-red

        _lineRenderer.material = laserMat;
        UpdateSize();
        _lineRenderer.widthCurve = AnimationCurve.Constant(0f, 1f, 0.5f); // change 0.5f to whatever thickness you want
        _lineRenderer.widthMultiplier = 1f;
    }
    public void UpdateLaser(Vector2 mousePosition)
    {
        Vector3 shootPoint = new Vector3(manager.shootPoint.transform.position.x, manager.shootPoint.transform.position.y, 0);

        Vector2 dir = mousePosition - (Vector2)shootPoint;
        RaycastHit2D hit = Physics2D.Raycast(shootPoint, dir.normalized, 50, _layerMask);

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

        if (!_isCostDelay&&_isActive)
        {
            if (playerManager.ConsumeMana(_usageCost))
            {
                StartCoroutine(ManaDelay());
            }
            else DisableLaser();
        }
    }
    public void EnableLaser(){
        if(playerManager.ManaCheck(_aciveCost)&&!skillIcon.CheckCoolDown(skillBar)&&!_isActive){
            playerManager.ConsumeMana(_aciveCost);
            UpdateSize();
            float pulse = 0.8f + Mathf.Sin(Time.time * 10f) * 0.2f;
            _lineRenderer.material.color = new Color(1f, 0.3f, 0f, pulse);
            _lineRenderer.enabled = true;
            _isActive = true;
            _hitbox.SetActive(true);
            playerManager.OnSkillStart();
            StartCoroutine(ManaDelay());
        }
    }
    private void UpdateSize()
    {
        float mod = manager.playerStat.spMod;
        _lineRenderer.widthMultiplier = 0.1f * mod;
    }
    public void DisableLaser(){
        _isActive = false;
        _lineRenderer.enabled = false;
        _hitbox.SetActive(false);
        skillIcon.SkillCooldown(skillBar);
        playerManager.OnSkillEnd();
    }
    
    IEnumerator ManaDelay()
    {
        _isCostDelay = true;
        yield return new WaitForSeconds(_costDelay);
        _isCostDelay = false;
        
    }
}
