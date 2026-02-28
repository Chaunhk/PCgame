using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExpManager : MonoBehaviour
{
    [SerializeField] private GameManager manager;
    [SerializeField] private EventControl eventControl;
    [SerializeField] private LevelExpSO levelExp;
    [SerializeField] private int currentExp;
    [SerializeField] private int lvUpExp;
    [SerializeField] private int currentLv;
    public GeneralBar expBar;
    void Start()
    {
        currentLv=1;
        lvUpExp=levelExp.baseExp;
        manager = GameManager.Instance;
        eventControl = manager.eventControl;
        expBar = manager.player.GetComponent<PlayerManager>().expBar;
        ResetExpBar();
    }
    private void LevelUp()
    {
        currentLv++;
        lvUpExp=levelExp.baseExp*currentLv*levelExp.expMod;
        eventControl.PassLevel();
        ResetExpBar();
    }
    public void GainExp(int expVal)
    {
        currentExp += expVal;
        expBar.Increase(expVal);
        if (currentExp >= lvUpExp) LevelUp();
    }
    public int GetLvUpValue()
    {
        return lvUpExp;
    }
    public void ResetExpBar()
    {
        expBar.ResetData(lvUpExp);
    }
}
