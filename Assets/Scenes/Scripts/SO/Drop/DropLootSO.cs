using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects//DropTable")]
public class DropTable : ScriptableObject
{
    public DropEntry[] drops;
}

[System.Serializable]
public class DropEntry
{
    public GameObject prefab;
    [Range(0, 1)]
    public float dropChance;
}
