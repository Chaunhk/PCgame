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
        this.moveSpeed = stat.moveSpeed;
        this.bodyRotatingSpeed = stat.bodyRotatingSpeed;
        this.headRotatingSpeed = stat.headRotatingSpeed;
    }
    //stats
    public int maxHealth;
    public int healthRegen;
    public int maxMana;
    public int manaRegen;
    public float attackRate; 
    public int damage;
    public int multiHit;
    public float spCost;
    public int spDamage;
    public void SetStat(PlayerStatSO stat){
        this.maxHealth = stat.maxHealth;
        this.healthRegen = stat.healthRegen;
        this.maxMana = stat.maxMana;
        this.manaRegen = stat.manaRegen;
        this.attackRate = stat.attackRate;
        this.damage = stat.damage;
        this.multiHit = stat.multiHit;
        this.spCost = stat.spCost;
        this.spDamage = stat.spDamage;
    }
}