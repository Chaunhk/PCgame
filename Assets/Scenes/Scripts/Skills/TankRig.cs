using UnityEngine;

/// <summary>
/// The parts of a tank prefab the rest of the game needs to find. Sits on the prefab root.
/// </summary>
// WHY: a tank is now its own prefab, spawned at run start, so nothing in the scene can hold a
// reference to its insides. Finding them by name ("ShootPoint") would break the first time someone
// renames a child in one tank and not the others. The prefab declares them instead.
public class TankRig : MonoBehaviour
{
    [SerializeField] private PlayerManager _playerManager;
    [Tooltip("Where projectiles leave from — the muzzle.")]
    [SerializeField] private Transform _shootPoint;
    [SerializeField] private TankSkillLoadout _loadout;

    public PlayerManager PlayerManager => _playerManager;
    public Transform ShootPoint => _shootPoint;
    public TankSkillLoadout Loadout => _loadout;

    /// <summary>Hand the tank the HUD widgets it cannot reference from a prefab.</summary>
    public void BindHud(GeneralBar healthBar, GeneralBar manaBar, GeneralBar expBar)
    {
        if (_playerManager == null) return;

        if (healthBar != null) _playerManager.healthBar = healthBar;
        if (manaBar != null) _playerManager.manaBar = manaBar;
        if (expBar != null) _playerManager.expBar = expBar;
    }

    private void Reset()
    {
        // convenience when adding this to an existing tank prefab
        _playerManager = GetComponentInChildren<PlayerManager>(true);
        _loadout = GetComponentInChildren<TankSkillLoadout>(true);

        ShootPointController controller = GetComponentInChildren<ShootPointController>(true);
        if (controller != null) _shootPoint = controller.transform;
    }
}
