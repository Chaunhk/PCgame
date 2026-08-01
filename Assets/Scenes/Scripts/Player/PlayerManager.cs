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
    public float basePickUp = 1f;
    public float pickUpRadius;
    public GameManager manager;
    //public ExpManager expManager;
    public GeneralBar healthBar;
    public GeneralBar manaBar;
    public GeneralBar expBar;
    [SerializeField] private int _manaRegen,_healthRegen;
    [SerializeField] private float _manaCooldown,_regenInterval,_skillUsageBlock,damagedBlock;
    [SerializeField] private bool _isManaRegenBlocked,_isHealthRegenBlocked;
    private float _healthRegenResumeAt;
    private void Start()
    {
        manager = GameManager.Instance;
        _playerStat = manager.playerStat;
        pickUpRadius = basePickUp;
        InitStat();

        // WHY: a regen interval of 0 makes WaitForSeconds yield every frame, which turns
        // "regen per interval" into "regen per frame" and refills the bar instantly. The
        // field is set in the inspector, so a fresh prefab or a reset component lands here.
        if (_regenInterval <= 0f) _regenInterval = 1f;

        StartCoroutine(ManaRegenLoop());
        StartCoroutine(HealthRegenLoop());
    }

    #region Initialize
    private void InitStat()
    {
        maxHealth = _playerStat.maxHealth;
        maxMana = _playerStat.maxMana;
        _healthRegen = _playerStat.healthRegen;
        _manaRegen = _playerStat.manaRegen;
        currentHealth = maxHealth;
        currentMana = maxMana;
        healthBar.InitData(maxHealth);
        manaBar.InitData(maxMana);
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

        pickUpRadius = basePickUp*_playerStat.spMod;
    }
    #endregion
    #region HP related 
    IEnumerator HealthRegenLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(_regenInterval);
            
            if (!_isHealthRegenBlocked && currentHealth < maxHealth)
            {
                currentHealth = Mathf.Min(currentHealth + _healthRegen, maxHealth);
                healthBar.Increase(_healthRegen);
            }
        }
    }
    // WHY: taking a second hit while this is already running must EXTEND the pause, not end it
    // early. With a plain WaitForSeconds, the first coroutine's timer expires on schedule and
    // clears the flag even though the player was hit again a moment ago, so regen resumes
    // mid-fight. Tracking a deadline instead makes overlapping hits behave the obvious way.
    IEnumerator DamagedDelay()
    {
        _isHealthRegenBlocked = true;
        _healthRegenResumeAt = Mathf.Max(_healthRegenResumeAt, Time.time + damagedBlock);

        while (Time.time < _healthRegenResumeAt) yield return null;

        _isHealthRegenBlocked = false;
    }
    public void Damage(int damageAmount)
    {
        currentHealth -= damageAmount;
        healthBar.Decrease(damageAmount);
        if (currentHealth <= 0)
        {
            Dead();
            return;
        }

        // WHY: health regen mirrors mana regen — mana pauses while a skill is used, health
        // pauses for a moment after being hit. DamagedDelay is what sets that pause, and it
        // had no caller, so _isHealthRegenBlocked was never true and the pause never existed.
        StartCoroutine(DamagedDelay());
    }
    public void Dead()
    {
        manager.eventControl.GameOverEvent();
    }
    #endregion
    
    #region ManaRelated
    
    IEnumerator ManaRegenLoop()
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
    // WHY: this used to spend and then return ManaCheck(val) — which answers "could I afford
    // ANOTHER one?", not "did this spend succeed?". Laser trusts the return value to decide
    // whether to keep the beam alive, so the beam cut out one full tick-cost early.
    public bool ConsumeMana(int val){
        if(!ManaCheck(val)) return false;

        currentMana -= val;
        manaBar.Decrease(val);
        return true;
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
        if (!collision.CompareTag("Exp")) return;

        ExpBehavior exp = collision.GetComponent<ExpBehavior>();
        if (exp != null && exp.CanStartPickup && Vector3.Distance(transform.position, collision.transform.position) <= pickUpRadius)
        {
            exp.StartMoving(transform);
        }
    }

    #endregion
}