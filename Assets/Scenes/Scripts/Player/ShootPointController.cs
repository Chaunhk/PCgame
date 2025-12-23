using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ShootPointController : MonoBehaviour
{
    [SerializeField] private GameManager _manager;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Vector3 _mousePos;
    [SerializeField] private bool _actionDelay;
    [SerializeField] private float _actionSpeed;
    [SerializeField] private float _chainHitDelay;
    private Laser laser;
    
    private void Start()
    {
        _manager = GameManager.Instance;
        _mainCamera = _manager.mainCamera;
        _actionSpeed = 1/_manager.playerStat.attackRate;
        
        _chainHitDelay = 0.1f;
        laser = _manager.laser.GetComponent<Laser>();
    }
    private void FixedUpdate()
    {
        
        _mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3 rotation = _mousePos - transform.position;
        float rotZ = Mathf.Atan2(rotation.y, rotation.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, rotZ);
        
    }
    private void Update()
    {
        Shoot();
        
    }
    private void Shoot(){
        if (Input.GetMouseButton(0) && !_actionDelay)
        {
            StartCoroutine(ShootBurst());
        }
        //Lase
        if (laser!=null){
            if(Input.GetKeyDown(KeyCode.E)){
                laser.EnableLaser();
            }
            if (Input.GetKey(KeyCode.E)){
                //if laser wasn't active, contantly check if it can active then enable it asap
                laser.UpdateLaser(_mousePos);
            }
            if(Input.GetKeyUp(KeyCode.E)){
                laser.DisableLaser();
            }
        }
        
    }
    private void SpawnBullet(){
        
        foreach (GameObject bullet in _manager.listBullet)
        {
            if (!bullet.activeSelf)
            {
                bullet.transform.position = _manager.shootPoint.transform.position;
                bullet.transform.rotation = transform.rotation;
                bullet.SetActive(true);
                break;
            }
        }
    }
    private IEnumerator ShootBurst() {
    _actionDelay = true;
    int hits = _manager.playerStat.multiHit;
    for (int i = 0; i < hits; i++) {
        SpawnBullet();
        yield return new WaitForSeconds(_chainHitDelay);
    }

    yield return new WaitForSeconds(_actionSpeed); // cooldown after burst
    _actionDelay = false;
}
}

