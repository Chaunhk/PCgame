using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search.Providers;
using UnityEngine;
namespace GameDataStruct
{
    [System.Serializable] 
    public struct SubPoint{
        public GameObject gameObject;
        public int waitTime;
    }
    [System.Serializable] 
    public struct SpawnPoint{
        public List<EnemyWave> waves;
        public int waitTime;
    }
    [System.Serializable] 
    public struct EnemyWave{
        public GameObject prefab;
        public int spawnDelay;
        public int spawnCap;
    }
    [System.Serializable]
    public struct UpgradeTemplate{
        public string name;
        public UpgradeCatergory catergory;
        public float value;
        public UpgradeTemplate(UpgradeCatergory c,string n, float v){
            catergory = c;
            name = n;
            value = v;
        }
        //above method allows to push params in
    }
    
} 
