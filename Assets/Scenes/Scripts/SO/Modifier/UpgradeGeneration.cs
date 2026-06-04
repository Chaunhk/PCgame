using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameDataStruct;
using TMPro;
using UnityEngine.UI;

public class UpgradeGeneration : MonoBehaviour
{
    private GameManager manager => GameManager.Instance;
    private UpgradeSO upgradeSO;
    public GameObject upgradeUI;

    [SerializeField] private List<GameObject> cardOrigins;
    [SerializeField] private List<GameObject> upgradeCards;
    [SerializeField] private GameObject hpUpgradeCard;
    [SerializeField] private GameObject mpUpgradeCard;
    [SerializeField] private GameObject skillUpgradeCard;
    [SerializeField] private GameObject attackUpgradeCard;
    [SerializeField] private List<UpgradeTemplate> randomUpgrades;

    public Dictionary<string, List<string>> allUpgrades = new Dictionary<string, List<string>>()
    {
        { "BasicAttack",   new List<string> { "attackRate", "attackDamage", "multiHit" } },
        { "SpecialAttack", new List<string> { "laserDamage", "manaCostRecduction" } },
        { "Health",        new List<string> { "maxHP", "hpRegeneration" } },
        { "Mana",          new List<string> { "maxMP", "mpRegeneration" } }
    };

    private void Start()
    {
        upgradeSO = manager.upgradeSO;
        UpgradeGenerate();
    }

    private void PrepareNextLevel()
    {
        manager.UpgradeCapCheck();
        foreach (GameObject card in upgradeCards)
            Destroy(card);
        UpgradeGenerate();
        manager.player.GetComponent<PlayerManager>().ApplyStatUpgrade();
        upgradeUI.SetActive(false);
        manager.eventControl.UnpauseEvent();
    }

    private void UpgradeGenerate()
    {
        List<UpgradeTemplate> upgrades = new List<UpgradeTemplate>();

        foreach (var kvp in allUpgrades)
        {
            UpgradeCatergory upgradeEnum = (UpgradeCatergory)System.Enum.Parse(typeof(UpgradeCatergory), kvp.Key);
            foreach (var upgradeName in kvp.Value)
            {
                float val = upgradeSO.GetValueByName(upgradeName);
                upgrades.Add(new UpgradeTemplate(upgradeEnum, upgradeName, val));
            }
        }

        randomUpgrades = upgrades.OrderBy(x => Random.value).Take(3).ToList();
        InitCard();
    }

    private void InitCard()
    {
        upgradeCards = new List<GameObject>();
        int i = 0;

        foreach (var upgrade in randomUpgrades)
        {
            switch (upgrade.catergory)
            {
                case UpgradeCatergory.BasicAttack:
                    SetupCard(attackUpgradeCard, cardOrigins[i], upgrade.name, upgrade.value);
                    break;
                case UpgradeCatergory.SpecialAttack:
                    SetupCard(skillUpgradeCard, cardOrigins[i], upgrade.name, upgrade.value);
                    break;
                case UpgradeCatergory.Health:
                    SetupCard(hpUpgradeCard, cardOrigins[i], upgrade.name, upgrade.value);
                    break;
                case UpgradeCatergory.Mana:
                    SetupCard(mpUpgradeCard, cardOrigins[i], upgrade.name, upgrade.value);
                    break;
            }
            i++;
        }
    }

    private void SetupCard(GameObject card, GameObject origin, string name, float val)
    {
        GameObject newCard = Instantiate(card, origin.transform.position, Quaternion.identity, origin.transform);

        TextMeshProUGUI text = newCard.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
            text.text = name;

        upgradeCards.Add(newCard);

        Button button = newCard.GetComponent<Button>();
        if (button != null)
        {
            switch (name)
            {
                case "attackRate":
                    button.onClick.AddListener(() => UpgradeAttackSpeed(val));
                    break;
                case "attackDamage":
                    button.onClick.AddListener(() => UpgradeAttack(val));
                    break;
                case "multiHit":
                    button.onClick.AddListener(() => UpgradeMultiHit(val));
                    break;
                case "laserDamage":
                    button.onClick.AddListener(() => UpgradeSPAttack(val));
                    break;
                case "manaCostRecduction":
                    button.onClick.AddListener(() => UpgradeManaCost(val));
                    break;
                case "maxHP":
                    button.onClick.AddListener(() => UpgradeHP(val));
                    break;
                case "hpRegeneration":
                    button.onClick.AddListener(() => UpgradeHPRegen(val));
                    break;
                case "maxMP":
                    button.onClick.AddListener(() => UpgradeMP(val));
                    break;
                case "mpRegeneration":
                    button.onClick.AddListener(() => UpgradeMPRegen(val));
                    break;
                default:
                    Debug.LogWarning($"No upgrade function found for: {name}");
                    break;
            }
            button.onClick.AddListener(() => PrepareNextLevel());
        }
        else Debug.Log("Button not found on prefab");
    }

    #region Upgrade Functions
    public void UpgradeAttack(float val)        { manager.playerStat.damage += (int)val; }
    public void UpgradeAttackSpeed(float val)   { manager.playerStat.attackRate -= val; }
    public void UpgradeSPAttack(float val)      { manager.playerStat.spDamage += (int)val; }
    public void UpgradeManaCost(float val)      { manager.playerStat.spCost -= (int)val; }
    public void UpgradeHP(float val)            { manager.playerStat.maxHealth += (int)val; }
    public void UpgradeHPRegen(float val)       { manager.playerStat.healthRegen += (int)val; }
    public void UpgradeMP(float val)            { manager.playerStat.maxMana += (int)val; }
    public void UpgradeMPRegen(float val)       { manager.playerStat.manaRegen += (int)val; }
    public void UpgradeMultiHit(float val)      { manager.playerStat.multiHit += (int)val; }
    #endregion
}