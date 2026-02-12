using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/LevelExp")]
public class LevelExpSO : ScriptableObject
{
    public int baseExp;
    public int expMod;
}
