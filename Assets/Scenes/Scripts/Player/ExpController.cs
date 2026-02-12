using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExpController : MonoBehaviour
{
    private float currentExp;
    private float lvUpExp;
    private float currentLv;
    private GameManager manager;
    private LevelManager levelManager;

    void Start()
    {
        manager = GameManager.Instance;
        levelManager = manager.gameObject.GetComponent<LevelManager>();
    }
    private void LevelUp()
    {
        levelManager.level++;
    }
    private void GainExp()
    {
        
    }
}
