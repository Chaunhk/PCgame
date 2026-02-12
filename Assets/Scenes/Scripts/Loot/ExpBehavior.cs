using UnityEngine;

public class ExpBehavior : MonoBehaviour
{
    private GameManager manager;
    private ExpManager expManager;
    public int expAmount = 1;
    public float moveSpeed = 10f;

    private bool isMovingToPlayer = false;
    private Transform player;
    void Start()
    {
        manager = GameManager.Instance;
        expManager = manager.expManager;
    }
    public void StartMoving(Transform target)
    {
        player = target;
        isMovingToPlayer = true;
    }

    private void Update()
    {
        if (!isMovingToPlayer || player == null) return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            player.position,
            moveSpeed * Time.deltaTime
        );

        // If very close → collect
        if (Vector3.Distance(transform.position, player.position) < 0.2f)
        {
            Collect();
        }
    }

    private void Collect()
    {
        
        gameObject.SetActive(false); // later replace with pool.Release(this)
        expManager.GainExp(expAmount);
    }
}
