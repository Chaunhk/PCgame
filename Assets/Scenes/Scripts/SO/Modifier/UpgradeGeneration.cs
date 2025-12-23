using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GameDataStruct;
using TMPro;
using System.Runtime.InteropServices.WindowsRuntime;
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
    [SerializeField] private GameObject laserUpgradeCard;
    [SerializeField] private GameObject attackUpgradeCard;
    [SerializeField] private List<UpgradeTemplate> randomUpgrades;
    public Dictionary<string, List<string>> allUpgrades = new Dictionary<string, List<string>>()
        {
            { "BasicAttack", new List<string> { "attackRate", "attackDamage", "multiHit" } },
            { "SpecialAttack", new List<string> { "laserDamage", "manaCostRecduction"} },
            { "Health", new List<string> { "maxHP", "hpRegeneration" } },
            { "Mana", new List<string> { "maxMP", "mpRegeneration" } }
        };
    // Start is called before the first frame update
    private void Start()
    {
        upgradeSO = manager.upgradeSO;
        UpgradeGenerate();
    }
    private void PrepareNextLevel(){
        manager.UpgradeCapCheck();
        foreach(GameObject card in upgradeCards){
            Destroy(card);
        }
        UpgradeGenerate();
        upgradeUI.SetActive(false);
        manager.levelManager.PrepareLevel();
    }
    private void UpgradeGenerate()
    {
        // need some break down here
        List<UpgradeTemplate> upgrades = new List<UpgradeTemplate>();
        // init new temp list of upgrade
        foreach (var kvp in allUpgrades){
            // loop through all key - value pairs
            foreach(var upgrade in kvp.Value){
                // loop through each values in key - value pairs
                UpgradeCatergory upgradeEnum = (UpgradeCatergory)System.Enum.Parse(typeof(UpgradeCatergory),kvp.Key.ToString());
                float val = GetUpgradeValue(upgrade);
                upgrades.Add(new UpgradeTemplate(upgradeEnum,upgrade,val));
                // parse every single value into the temp list 
            }
        }
        randomUpgrades = upgrades.OrderBy(x => Random.value).Take(3).ToList();
        //from the list, shuffle the values and take out 3 top value
        InitCard();
    }
    public float GetUpgradeValue(string name){
        foreach (UpgradeTemplate upgrade in upgradeSO.upgrades){
            if (upgrade.name == name){
                return upgrade.value;
            }
        }
        Debug.Log("Cannot find value");
        return 0;
        
    }
    private void InitCard(){
        int i=0;
        upgradeCards = new List<GameObject>();
        //
        foreach (var upgrade in randomUpgrades){
            switch(upgrade.catergory){
                case UpgradeCatergory.BasicAttack:
                    SetupCard(attackUpgradeCard,cardOrigins[i],upgrade.name,upgrade.value);
                    break;
                case UpgradeCatergory.SpecialAttack:
                    SetupCard(laserUpgradeCard,cardOrigins[i],upgrade.name,upgrade.value);
                    break;
                case UpgradeCatergory.Health:
                    SetupCard(hpUpgradeCard,cardOrigins[i],upgrade.name,upgrade.value);
                    break;
                case UpgradeCatergory.Mana:
                    SetupCard(mpUpgradeCard,cardOrigins[i],upgrade.name,upgrade.value);
                    break;
            }
            i++;
        }
    }
    private void SetupCard(GameObject card, GameObject origin, string name,float val){
        GameObject newCard = GameObject.Instantiate(card,origin.transform.position,Quaternion.identity,origin.transform);
        TextMeshProUGUI text = newCard.GetComponentInChildren<TextMeshProUGUI>();
        if(text!=null){
            text.text = name;// make upgrade skill more reconizable
        }
        upgradeCards.Add(newCard);
        Button button = newCard.GetComponent<Button>();
        if(button!=null){
            for(int i=0;i<upgradeSO.upgrades.Count;i++){
                if(upgradeSO.upgrades[i].name==name){
                    switch (i){
                        case 0:
                        button.onClick.AddListener(() =>  UpgradeAttackSpeed(val));
                        break;
                        case 1:
                        button.onClick.AddListener(() =>  UpgradeAttack(val));
                        break;   
                        case 2:
                        button.onClick.AddListener(() =>  UpgradeMultiHit(val));
                        break;
                        case 3:
                        button.onClick.AddListener(() =>  UpgradeSPAttack(val));
                        break;
                        case 4:
                        button.onClick.AddListener(() =>  UpgradeManaCost(val));
                        break;
                        case 5:
                        button.onClick.AddListener(() =>  UpgradeHP(val));
                        break;  
                        case 6:
                        button.onClick.AddListener(() =>  UpgradeHPRegen(val));
                        break;
                        case 7:
                        button.onClick.AddListener(() =>  UpgradeMP(val));
                        break;
                        case 8:  
                        button.onClick.AddListener(() =>  UpgradeMPRegen(val));
                        break; 
                    }
                    button.onClick.AddListener(() => PrepareNextLevel());
                    Debug.Log(i);
                }
            }
        }
        else Debug.Log("Button not found on prefab");
    }

    public void UpgradeAttack(float val){
        manager.playerStat.damage += (int)val;
    }
    public void UpgradeAttackSpeed(float val){
        manager.playerStat.attackRate -= val;
    }
    public void UpgradeSPAttack(float val){
        manager.playerStat.spDamage += (int)val;
    }
    public void UpgradeManaCost(float val){
        manager.playerStat.spCost -= val;
    }
    public void UpgradeHP(float val){
        manager.playerStat.maxHealth += (int)val;
    }
    public void UpgradeHPRegen(float val){
        manager.playerStat.healthRegen += (int)val;
    }
    public void UpgradeMP(float val){
        manager.playerStat.maxMana += (int)val;
    }
    public void UpgradeMPRegen(float val){
        manager.playerStat.manaRegen += (int)val;
    }
    public void UpgradeMultiHit(float val){
         manager.playerStat.multiHit += (int)val;
    }
}
