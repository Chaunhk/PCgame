using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using GameDataStruct;
using UnityEditorInternal;
public class SpawnPointControl : MonoBehaviour
{
    private GameManager manager => GameManager.Instance;
    [SerializeField] private List<SubPoint> _subPoints;
    [SerializeField] private SpawnPoint _spawnPoint;
    
    public GameObject mobPrefab;
    [Header("Wave Data")]
    [SerializeField] private int _wave;
    [SerializeField] private EnemyWave enemyWave;
    [SerializeField] private bool _isSpawnable;
    [SerializeField] private float _spawnDelay;
    [SerializeField] private float _currentSpawnDelay;
    [SerializeField] private int _spawnCount; 
    [SerializeField] private int _spawnCap; 
    private void Start()
    {
        //todo: somehow make the level resstart properly
        InitSpawnData();
    }
    private void Update(){
        SpawnWave();
    }
    public void InitSpawnData(){
        
        manager.isSpawnEnd = false;
        _wave = 0;
        _spawnCount = 0;
        SetWaveData();
    }
    public void SetWaveData()
    {
        _spawnCount = 0;
        if ( _spawnPoint.waves == null || _spawnPoint.waves.Count == 0)
        {
            Debug.Log("SetWaveData Error: _spawnPoint or waves list is null/empty!");
            _isSpawnable = false;
            return;
        }
        //enemyWave = new EnemyWave();
        enemyWave = _spawnPoint.waves[_wave];
        _spawnDelay = enemyWave.spawnDelay;
        _spawnCap = enemyWave.spawnCap;
        mobPrefab = enemyWave.prefab;
        _isSpawnable = true;
    }
    public void SpawnWave(){
        
        if ( _isSpawnable == true && _spawnCount < _spawnCap){
            Debug.Log("Spawn enemy");
            GameObject enemy = Instantiate(mobPrefab, transform.position, Quaternion.identity, transform);
            manager.enemyCount++;
            EnemyMovement enemyMovement = enemy.GetComponent<EnemyMovement>();
            if (enemyMovement != null){
                enemyMovement.SetPath(UpdatePath());
            }
            _spawnCount++;
            
            if(_spawnCount==_spawnCap){
                StartCoroutine(WaveCountDown());
            }
            else StartCoroutine(SpawnCountDown());
        }
        
    }
    private List<SubPoint> UpdatePath(){
        List<SubPoint> desPoints = new List<SubPoint>();
        for(int i = 0; i < _subPoints.Count; i++){
            desPoints.Add(_subPoints[i]);
        }
        return desPoints;
    }
    IEnumerator SpawnCountDown(){
        _isSpawnable = false;
        yield return new WaitForSeconds(_spawnDelay);
        _isSpawnable = true;
    }  
    IEnumerator WaveCountDown(){
        bool returBool = false;
        _isSpawnable = false;
        _wave++;
        if(_wave<_spawnPoint.waves.Count) {
            SetWaveData();
            returBool = true;
        }
        else
        {
            manager.isSpawnEnd = true;
        }
        yield return new WaitForSeconds(_spawnPoint.waitTime);
        _isSpawnable = returBool;
    }
}