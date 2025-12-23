using System.Collections;
using System.Collections.Generic;
using GameDataStruct;
using UnityEngine;
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Upgrade")]
public class UpgradeSO : ScriptableObject
{
    // [Header("Basic Attack")]
    // public float attackRate;
    // public int attackDamage;
    // [Header("Special Attack")]
    // public int laserDamage;
    // public int manaCostRecduction;
    // [Header("Health")]
    // public int maxHP;
    // public int hpRegeneration;
    // [Header("Mana")]
    // public int maxMP;
    // public int mpRegeneration;
    public List<UpgradeTemplate> upgrades;
    // public float GetValueByName(string name){
    //     foreach (var val in UpgradeSO)
    // }
}
public enum UpgradeCatergory{
    BasicAttack,
    Health,
    SpecialAttack,
    Mana
}
