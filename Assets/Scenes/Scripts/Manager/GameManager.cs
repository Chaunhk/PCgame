using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public class GameManager : MonoBehaviour
{
    #region singleton
    public static GameManager Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null){
            Instance  = this;
         DontDestroyOnLoad(gameObject);
}
        else
{
    Destroy(gameObject);
}   
    }
    #endregion
    public enum GameModes
    {
        TD,
        VS,
    }
    public GameModes currentGameMode;
    public EventControl eventControl;
    public LevelManager levelManager;
    public ExpManager expManager;
    public Camera mainCamera;
    public GameObject player;
    public PlayerStatSO playerBaseStat;
    public PlayerStatSO playerStat;
    public PlayerStatSO playerStatCap;
    public int minDistance;
    public GameObject shootPoint;
    public UpgradeSO upgradeSO;
      // change this param into playerstatSO
    public GameObject[] listBullet;
    public GameObject laser;
    public int enemyCount;
    public bool isSpawnEnd;
    //public TMPro.TextMeshProUGUI chatText;
    //public TextMeshProUGUI hpText, manaText;
    //[SerializeField] private GeneralMenuController generalMenuController;
    private void Start()
    {
        InitGameStat();
    }
    private void InitGameStat()
    {
        isSpawnEnd = false;
        playerStat.SetStat(playerBaseStat);
    }
    public void UpgradeCapCheck(){
        //if stat gonna increase, use min and if stat gonna decrease, use max
        playerStat.maxHealth = Mathf.Min(playerStat.maxHealth, playerStatCap.maxHealth);
        playerStat.healthRegen = Mathf.Min(playerStat.healthRegen, playerStatCap.healthRegen);
        playerStat.maxMana = Mathf.Min(playerStat.maxMana, playerStatCap.maxMana);
        playerStat.manaRegen = Mathf.Min(playerStat.manaRegen, playerStatCap.manaRegen);
        playerStat.attackRate = Mathf.Max(playerStat.attackRate, playerStatCap.attackRate);
        playerStat.damage = Mathf.Min(playerStat.damage, playerStatCap.damage);
        playerStat.multiHit = Mathf.Min(playerStat.multiHit,playerStatCap.multiHit);
        playerStat.spCost = Mathf.Max(playerStat.spCost, playerStatCap.spCost);
        playerStat.spDamage = Mathf.Min(playerStat.spDamage, playerStatCap.spDamage);
    }
}

//sumary:
//make laser minus mana imidiately instead if 1s delay
//spawn counter doesn't work properly which lead to level ends early
//
