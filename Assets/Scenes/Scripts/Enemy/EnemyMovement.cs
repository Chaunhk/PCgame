using System.Collections;
using System.Collections.Generic;
using GameDataStruct;
using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private GameManager manager;
    private PlayerManager player;
    [SerializeField] private List<SubPoint> path;
    [SerializeField] private int index;
    [SerializeField] private Transform target;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private GeneralEnemy enemy;
    private void Start()
    {
        manager = GameManager.Instance;
        player = manager.player.GetComponent<PlayerManager>();
        switch (manager.currentGameMode){
            case GameManager.GameModes.TD: 
                target = path[index].gameObject.transform;
                break;
            case GameManager.GameModes.VS: 
                target = manager.player.transform;
                break;
        }
        rb = gameObject.GetComponent<Rigidbody2D>();
        enemy = gameObject.GetComponent<GeneralEnemy>();
    }

    private void Update()
    {
        if (Vector2.Distance(target.position, transform.position) <= 0.1f)
        {
            index++;
            if (index == path.Count) // when mob reached the end of route and switch target to player
            {
                target = manager.player.transform;
                return;
            }
            else if (index > path.Count) // when mob reached player
            {
                enemy.DeSpawn();
                player.Damage(enemy.enemyStat.objectiveCost);
            }
            else //when mob still in it's route
            {
                target = path[index].gameObject.transform;
            }
        }
    }
    private void FixedUpdate()
    {
        Vector2 dir = (target.position - transform.position).normalized;

        rb.velocity = enemy.speed * dir; 
    }
    public void SetPath (List<SubPoint> newpath){
        path.Clear();
        path = newpath;
    }
}
