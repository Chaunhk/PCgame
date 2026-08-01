using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootPointController : MonoBehaviour
{
    [SerializeField] private GameManager _manager;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Vector3 _mousePos;
    [SerializeField] private bool _actionDelay;
    [SerializeField] private float _actionSpeed;
    [SerializeField] private float _chainHitDelay;
    [SerializeField] private Canon canon;
    [SerializeField] private Laser laser;
    [SerializeField] private GameObject _bulletPrefab;
    
    private void Start()
    {
        _manager = GameManager.Instance;
        _mainCamera = _manager.mainCamera;
        _actionSpeed = 1/_manager.playerStat.attackRate;
        
        _chainHitDelay = 0.1f;
        laser = _manager.laser.GetComponent<Laser>();
    }
    // WHY: aim used to be computed in FixedUpdate, which runs at the physics rate (50 Hz by
    // default) while rendering and input run per frame. The turret visibly trailed the cursor,
    // and anything reading _mousePos from Update — shooting, and the laser — aimed at a point
    // up to one physics step stale. Aim is input, not physics, so it belongs here.
    private void Aim()
    {
        Vector3 raw = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        _mousePos = new Vector3(raw.x, raw.y, 0);

        Vector3 rotation = _mousePos - transform.position;
        float rotZ = Mathf.Atan2(rotation.y, rotation.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, rotZ);
    }
    private void Update()
    {
        Aim();
        Shoot();
    }
    private void Shoot(){
        if (Input.GetMouseButton(0) && !_actionDelay)
        {
            StartCoroutine(ShootSpread());
            // StartCoroutine(ShootBurst());
        }
        if(Input.GetKeyDown(KeyCode.E)){
            canon.EnableFire(transform);
        }
        //Laser
        if (laser!=null){
            if(Input.GetKeyDown(KeyCode.Q)){
                laser.EnableLaser();
                //canon.EnableFire(transform);
            }
            if (Input.GetKey(KeyCode.Q)){
                //if laser wasn't active, contantly check if it can active then enable it asap
                laser.UpdateLaser(_mousePos);
            }
            if(Input.GetKeyUp(KeyCode.Q)){
                laser.DisableLaser();
            }
        }
        
    }
    private void SpawnBullet(float angle = 0f){
        // WHY: this used to scan listBullet for an inactive entry and simply do nothing when the
        // array was exhausted — a shot that vanished with no bullet and no error. The pool grows
        // instead, and says so in the console if the prefab was never registered.
        Quaternion rotation = transform.rotation * Quaternion.Euler(0, 0, angle);
        _manager.projectilePool.Spawn(_bulletPrefab, _manager.shootPoint.transform.position, rotation);
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
    
    private IEnumerator ShootSpread(){
        _actionDelay = true;
        int hits = _manager.playerStat.multiHit;
        float spreadAngle = 15f; // Adjust this value for the desired spread angle
        for (int i = 0; i < hits; i++) {
            float angle = spreadAngle * (i - (hits - 1) / 2f);
            SpawnBullet(angle);
        }
        yield return new WaitForSeconds(_actionSpeed);
        _actionDelay = false;
    }
}

