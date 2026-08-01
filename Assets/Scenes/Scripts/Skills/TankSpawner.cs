using UnityEngine;

/// <summary>
/// Spawns the tank the player picked, and connects it to the scene.
/// </summary>
// WHY: each tank is its own prefab now, so the gameplay scene cannot hold the player any more — it
// holds a spawn point and the HUD, and the tank arrives at run start. Everything that used to be
// wired by hand in the scene (GameManager.player, the muzzle, the health and mana bars) is wired
// here instead, because a prefab cannot reference a scene object.
//
// This runs in Awake on purpose: several Start methods read GameManager.player and cache it —
// the camera follow caches the transform it chases — so the tank has to exist before any Start runs.
[DefaultExecutionOrder(-100)]
public class TankSpawner : MonoBehaviour
{
    [Header("Which tank")]
    [SerializeField] private TankRosterSO _roster;
    [Tooltip("Forces one tank, ignoring the roster and the player's choice. For testing.")]
    [SerializeField] private TankDefinitionSO _tankOverride;

    [Header("Where")]
    [Tooltip("Spawn position. Falls back to this object's own position.")]
    [SerializeField] private Transform _spawnPoint;
    [Tooltip("A tank already sitting in the scene. Removed once the real one is spawned.")]
    [SerializeField] private GameObject _placeholder;

    [Header("HUD to hand the tank")]
    [SerializeField] private GeneralBar _healthBar;
    [SerializeField] private GeneralBar _manaBar;
    [SerializeField] private GeneralBar _expBar;

    public TankDefinitionSO SpawnedTank { get; private set; }
    public TankRig SpawnedRig { get; private set; }

    private void Awake()
    {
        GameManager manager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (manager == null)
        {
            Debug.LogError($"{nameof(TankSpawner)}: no GameManager in the scene.");
            return;
        }

        TankDefinitionSO tank = _tankOverride != null ? _tankOverride : TankSelection.Resolve(_roster);
        if (tank == null)
        {
            Debug.LogError($"{nameof(TankSpawner)}: no tank to spawn — the roster is empty or unassigned.");
            return;
        }

        if (tank.bodyPrefab == null)
        {
            Debug.LogError($"{nameof(TankSpawner)}: tank '{tank.tankId}' has no body prefab.");
            return;
        }

        SpawnedTank = tank;

        // WHY: assigned before GameManager.Start runs, because that is what copies the base stats
        // into the live ones. Setting the live stats directly here would be overwritten a moment later.
        if (tank.baseStats != null) manager.playerBaseStat = tank.baseStats;

        Vector3 position = _spawnPoint != null ? _spawnPoint.position
            : _placeholder != null ? _placeholder.transform.position
            : transform.position;

        if (_placeholder != null) Destroy(_placeholder);

        GameObject spawned = Instantiate(tank.bodyPrefab, position, Quaternion.identity);
        spawned.name = tank.bodyPrefab.name;

        TankRig rig = spawned.GetComponent<TankRig>();
        if (rig == null)
        {
            Debug.LogError($"{nameof(TankSpawner)}: '{tank.bodyPrefab.name}' has no {nameof(TankRig)}, so nothing can be connected to it.");
            return;
        }

        SpawnedRig = rig;

        manager.player = rig.PlayerManager != null ? rig.PlayerManager.gameObject : spawned;
        if (rig.ShootPoint != null) manager.shootPoint = rig.ShootPoint.gameObject;

        rig.BindHud(_healthBar, _manaBar, _expBar);
        if (rig.Loadout != null) rig.Loadout.SetTank(tank);
    }
}
