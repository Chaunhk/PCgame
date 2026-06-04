using System.Collections.Generic;
using GameDataStruct;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Upgrade")]
public class UpgradeSO : ScriptableObject
{
    public List<UpgradeTemplate> upgrades;

    public float GetValueByName(string name)
{
    foreach (UpgradeTemplate upgrade in upgrades)
    {
        if (upgrade.name == name)
            return upgrade.value;
    }
    Debug.LogWarning($"Upgrade '{name}' not found in UpgradeSO");
    return 0f;
}
}

public enum UpgradeCatergory
{
    BasicAttack,
    SpecialAttack,
    Health,
    Mana
}