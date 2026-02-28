using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventControl : MonoBehaviour
{
    //private GameManager manager;
    [SerializeField] GameObject UpgradePanel;
    [SerializeField] GameObject GameOverPanel;
    //[SerializeField] private bool isPaused = false;
    private void Start(){
        //manager = GameManager.Instance;
    }
    
    public void GameOverEvent(){
        GameOverPanel.SetActive(true);
    }
    public void PassLevel(){
        PauseEvent();
        UpgradePanel.SetActive(true);
    }
    public void PauseEvent()
    {
        Time.timeScale = 0f;
        //isPaused = true;
    }
    public void UnpauseEvent()
    {
        Time.timeScale = 1f;
        //isPaused = false;
    }
}
