using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/EnemyStat")]
public class EnemyStatSO : ScriptableObject
{
    public int objectiveCost;
    public int maxHealth;
    public int speed;
}
