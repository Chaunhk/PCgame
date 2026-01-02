using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamFolow : MonoBehaviour
{
    private GameManager manager;
    private Transform player;
    private Transform cam;
    // Start is called before the first frame update
    void Start()
    {
        manager = GameManager.Instance;
        player = manager.player.transform;
        cam = manager.mainCamera.transform;
    }

    // Update is called once per frame
    void Update()
    {
        //transform.LookAt(player);
        Vector3 pos = new Vector3(player.position.x, player.position.y, transform.position.z-1);
        cam.transform.position = pos;
    }
}
