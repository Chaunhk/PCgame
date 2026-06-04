using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/PlayerStat")]
public class PlayerStatSO : ScriptableObject
{
    //movements
    public float moveSpeed;
    public float bodyRotatingSpeed;
    public float headRotatingSpeed;
    public void SetMovement(PlayerStatSO stat){
        moveSpeed = stat.moveSpeed;
        bodyRotatingSpeed = stat.bodyRotatingSpeed;
        headRotatingSpeed = stat.headRotatingSpeed;
    }
    //stats
    public int maxHealth;
    public int healthRegen;
    public int maxMana;
    public int manaRegen;
    public float attackRate; 
    public int damage;
    public int multiHit;
    public float spMod;
    public int spCost;
    public int spDamage;
    public void SetStat(PlayerStatSO stat){
        moveSpeed = stat.moveSpeed;
        maxHealth = stat.maxHealth;
        healthRegen = stat.healthRegen;
        maxMana = stat.maxMana;
        manaRegen = stat.manaRegen;
        attackRate = stat.attackRate;
        damage = stat.damage;
        multiHit = stat.multiHit;
        spCost = stat.spCost;
        spMod = stat.spMod;
        spDamage = stat.spDamage;
    }
}