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
    // WHY: listBullet/listFire (51 and 11 hand-placed scene objects, searched linearly for a
    // free one on every shot) are replaced by ProjectilePool, which is the same UnityEngine.Pool
    // shape MobPoolManager already uses for enemies.
    public ProjectilePool projectilePool;

    [Header("Tank")]
    [Tooltip("Every tank a player can pick.")]
    public TankRosterSO tankRoster;
    // WHY: one visible place to answer "which tank am I taking in?", rather than it being buried in
    // a spawner component or in saved player prefs. Leaving it empty means the run uses whatever the
    // player picked, and the roster's default if they never picked — which is what will happen once
    // a select screen exists.
    [Tooltip("Force a tank for this run. Leave empty to use the player's saved choice, then the roster default.")]
    public TankDefinitionSO selectedTank;

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
        playerStat.spMod = Mathf.Min(playerStat.spMod, playerStatCap.spMod);
        playerStat.spDamage = Mathf.Min(playerStat.spDamage, playerStatCap.spDamage);
    }
}

