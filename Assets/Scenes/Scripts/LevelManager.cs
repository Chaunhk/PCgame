using System.Collections;
using System.Collections.Generic;
using GameDataStruct;
using UnityEngine;
public class LevelManager : MonoBehaviour
{
    private GameManager manager;
    public List<GameObject> spawnPoints; 
    public List<GameObject> activePoints; 
    //there's 8 spawn point; roll randomly and set active spawn point up to lv;
    //somehow only allow 2 spawn point spawns at a time, 
    public List<GameObject> subDesPoints;
    public List<GameObject> mobPrefabs;
    public int level;
    public float statScale;
    private void Start()
    {
        manager = GameManager.Instance;
        level=0;
        manager.levelManager = this;
        PrepareLevel();
    }
    public void PrepareLevel()
    {
        level ++;

        SpawnPointSetup() ;
    }
    private void SpawnPointSetup(){
        foreach (GameObject sp in spawnPoints) {
            sp.SetActive(false); // reset all
        }
        activePoints.Clear();  
        List<GameObject> points = new List<GameObject>(spawnPoints);
         //Debug.Log("Level:"+level+";count="+points.Count);
        int loop = Mathf.Min(level,points.Count);
        for (int i = 0;i<loop;i++){
            int selectedIndex = Random.Range(0,points.Count);
            //Debug.Log("Index:"+selectedIndex+";i="+i);
            GameObject selectedPoint = points[selectedIndex];
            activePoints.Add(selectedPoint);
            points.RemoveAt(selectedIndex);
        }
        foreach (GameObject spawnPoint in activePoints){
            SpawnPointControl spawner =  spawnPoint.GetComponent<SpawnPointControl>();
            if (spawner != null){
                spawner.InitSpawnData();
                spawnPoint.SetActive(true);
            }
            
        }
    }
}
