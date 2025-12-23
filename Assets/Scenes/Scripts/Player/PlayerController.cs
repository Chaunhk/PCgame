using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour, IDamageable
{
    private PlayerStatSO _playerStat;
    public int maxHealth;
    public int currentHealth;
    public int maxMana;
    public int currentMana;
    public GameManager manager;
    public GeneralBar healthBar;
    public GeneralBar manaBar;
    [SerializeField] private int _manaRegen;
    [SerializeField] private float _manaCooldown,_regenInterval,_skillUsageBlock;
    [SerializeField] private bool _isManaRegenBlocked,_isUsingSkill;
    private void Start()
    {
        manager = GameManager.Instance;
        _playerStat = manager.playerStat;
        InitStat();
        //gameObject.SetActive(false);
    }
    private void Update()
    {
        RegenerateMana();
    }
    private void FixedUpdate()
    {
        if(!_isUsingSkill&&_manaCooldown>0)
            _manaCooldown-=Time.deltaTime;
        if(_manaCooldown<=0)
            _isManaRegenBlocked = false;
    }
    #region Initialize
    private void InitStat()
    {
        maxHealth = _playerStat.maxHealth;
        maxMana = _playerStat.maxMana;
        currentHealth = maxHealth;
        currentMana = maxMana;
        _isUsingSkill = false;
        // minDistance = manager.minDistance;
        // speed = enemyStat.speed;
        // dir = manager.player.transform.position - transform.position;
        healthBar.InitData(maxHealth);
        manaBar.InitData(maxMana);
    }
    #endregion
    // todo: put hp regen here and make it work with upgrade
    #region HP related 
    public void Damage(int damageAmount)
    {
        currentHealth -= damageAmount;
        healthBar.Decrease(damageAmount);
        if (currentHealth <= 0)
        {
            Dead();
            return;
        }
        
    }
    public void Dead()
    {
        manager.eventControl.GameOverEvent();
    }
    #endregion
    
    #region ManaRelated
    
    private void RegenerateMana(){
        if(!_isManaRegenBlocked&&currentMana < maxMana){
            currentMana += _manaRegen;
            if (currentMana > maxMana) {
                currentMana = maxMana;
                _manaCooldown = -1;
            }
                
            manaBar.Increase(_manaRegen);
            _manaCooldown = _regenInterval;
            _isManaRegenBlocked = true;
        }
    }
    public void OnSkillStart(){
        _isUsingSkill = true;
    }
    public void OnSkillEnd(){
        _isUsingSkill = false;
        _manaCooldown = _skillUsageBlock;
    }
    public bool ManaCheck(int val){
        if (val > currentMana){
            return false;
        }
        else return true;
    }
    public bool ConsumeMana(int val){
        if(ManaCheck(val)){
            currentMana -= val;
            manaBar.Decrease(val);
        }
        return ManaCheck(val);
    }
    #endregion
}