using UnityEngine;

public class ExpBehavior : MonoBehaviour
{
    private enum PickupState
    {
        Idle,
        MovingAway,
        MovingToPlayer
    }

    private GameManager manager;
    private ExpManager expManager;
    public int expAmount;
    public float moveSpeed;
    [SerializeField] private float pickupRadius = 1.5f;
    [SerializeField] private float anticipationDistance = 0.8f;
    [SerializeField] private float pickupSequenceDuration = 0.3f;
    private float _speed;
    private PickupState pickupState = PickupState.Idle;
    private Transform player;
    private Vector3 anticipationTarget;
    private float pickupSequenceTimer;
    private bool hasPickupStarted;
    private bool isCollected;

    public bool CanStartPickup
    {
        get { return !isCollected && !hasPickupStarted && pickupState == PickupState.Idle; }
    }

    private void Start()
    {
        manager = GameManager.Instance;
        expManager = manager.expManager;
        if (manager != null && manager.player != null)
        {
            player = manager.player.transform;
        }
    }

    private void OnEnable()
    {
        float minSpeed = moveSpeed * .8f;
        float maxSpeed = moveSpeed * 1.2f;
        _speed = Random.Range(minSpeed, maxSpeed);
        pickupState = PickupState.Idle;
        hasPickupStarted = false;
        isCollected = false;
        pickupSequenceTimer = 0f;
    }

    public void StartMoving(Transform target)
    {
        if (target == null || !CanStartPickup) return;

        player = target;
        hasPickupStarted = true;
        Vector3 direction = (transform.position - player.position).normalized;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.right;
        }

        anticipationTarget = transform.position + direction * anticipationDistance;
        pickupSequenceTimer = 0f;
        pickupState = PickupState.MovingAway;
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        if (pickupState == PickupState.MovingAway)
        {
            transform.position = Vector3.MoveTowards(transform.position, anticipationTarget, _speed * Time.deltaTime);
            pickupSequenceTimer += Time.deltaTime;

            if (pickupSequenceTimer >= pickupSequenceDuration)
            {
                pickupState = PickupState.MovingToPlayer;
            }
            return;
        }

        if (pickupState == PickupState.MovingToPlayer)
        {
            transform.position = Vector3.MoveTowards(transform.position, player.position, _speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, player.position) < 1f)
            {
                Collect();
            }
            return;
        }

        if (CanStartPickup && Vector3.Distance(transform.position, player.position) <= GetPickupRadius())
        {
            StartMoving(player);
        }
    }

    private float GetPickupRadius()
    {
        if (player != null)
        {
            PlayerManager playerManager = player.GetComponent<PlayerManager>();
            if (playerManager != null && playerManager.pickUpRadius > 0.01f)
            {
                return playerManager.pickUpRadius;
            }
        }

        return pickupRadius;
    }

    private void Collect()
    {
        if (isCollected) return;

        isCollected = true;
        gameObject.SetActive(false);
        expManager.GainExp(expAmount);
    }
}
