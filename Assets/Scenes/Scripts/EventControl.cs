using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventControl : MonoBehaviour
{
    private GameManager manager;
    [SerializeField] GameObject UpgradePanel;
    [SerializeField] GameObject GameOverPanel;
    private void Start(){
        manager = GameManager.Instance;
    }
    
    public void GameOverEvent(){
        GameOverPanel.SetActive(true);
    }
    public void PassLevel(){
        UpgradePanel.SetActive(true);
    }
}
