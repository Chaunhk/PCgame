using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerManager : MonoBehaviour, IDamageable
{
    private PlayerStatSO _playerStat;
    public int maxHealth;
    public int currentHealth;
    public int maxMana;
    public int currentMana;
    public float pickUpRadius;
    public GameManager manager;
    //public ExpManager expManager;
    public GeneralBar healthBar;
    public GeneralBar manaBar;
    public GeneralBar expBar;
    [SerializeField] private int _manaRegen;
    [SerializeField] private float _manaCooldown,_regenInterval,_skillUsageBlock;
    [SerializeField] private bool _isManaRegenBlocked;
    private void Start()
    {
        manager = GameManager.Instance;
        _playerStat = manager.playerStat;
        InitStat();
        StartCoroutine(RegenLoop());  
    }

    #region Initialize
    private void InitStat()
    {
        maxHealth = _playerStat.maxHealth;
        maxMana = _playerStat.maxMana;
        _manaRegen = _playerStat.manaRegen;
        currentHealth = maxHealth;
        currentMana = maxMana;
        //_isUsingSkill = false;
        // minDistance = manager.minDistance;
        // speed = enemyStat.speed;
        // dir = manager.player.transform.position - transform.position;
        healthBar.InitData(maxHealth);
        manaBar.InitData(maxMana);
        //expManager.ResetExpBar();
    }
    #endregion
    #region Upgrade
    public void ApplyStatUpgrade()
    {
        // Health
        int newMaxHealth = _playerStat.maxHealth;
        int healthDiff = newMaxHealth - maxHealth;
        maxHealth = newMaxHealth;
        currentHealth = Mathf.Min(currentHealth + healthDiff, maxHealth);
        healthBar.InitData(maxHealth);
        healthBar.SetValue(currentHealth); // sync bar to actual current value

        // Mana
        int newMaxMana = _playerStat.maxMana;
        int manaDiff = newMaxMana - maxMana;
        maxMana = newMaxMana;
        currentMana = Mathf.Min(currentMana + manaDiff, maxMana);
        manaBar.InitData(maxMana);
        manaBar.SetValue(currentMana);

        // Mana regen
        _manaRegen = _playerStat.manaRegen;
    }
    #endregion
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
    
    IEnumerator RegenLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(_regenInterval);
            
            if (!_isManaRegenBlocked && currentMana < maxMana)
            {
                currentMana = Mathf.Min(currentMana + _manaRegen, maxMana);
                manaBar.Increase(_manaRegen);
            }
        }
    }
    public void OnSkillStart(){
        _isManaRegenBlocked = true;
        
    }
    public void OnSkillEnd()
    {
        StartCoroutine(SkillEndDelay());
    }

    IEnumerator SkillEndDelay()
    {
        _isManaRegenBlocked = true;
        yield return new WaitForSeconds(_skillUsageBlock);
        _isManaRegenBlocked = false;
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
    // IEnumerator ManaDelay()
    // {
    //     _isManaRegenBlocked = true;
    //     yield return new WaitForSeconds(_manaCooldown);
    //     _isManaRegenBlocked = false;
        
    // }
    #endregion
    #region Pickup
    void OnTriggerEnter2D(Collider2D collision)
    {
        //Debug.Log("Got "+ collision.tag);
        if (collision.CompareTag("Exp"))
        {
            ExpBehavior exp = collision.GetComponent<ExpBehavior>();
            exp.StartMoving(transform);
        }
    }
    #endregion
}