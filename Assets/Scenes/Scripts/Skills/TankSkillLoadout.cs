using System;
using UnityEngine;

/// <summary>
/// The tank's three slots: reads input, refreshes aim once per frame, drives the slots.
/// </summary>
// WHY: the tank used to hold Canon and Laser as named fields with hardcoded keys, so a third skill
// meant editing this file. Nothing here knows any skill by name — slots hold assets, and the keys
// come from a list.
public class TankSkillLoadout : MonoBehaviour
{
    [Header("Loadout")]
    [Tooltip("Every tank the player can pick. The chosen one is used at run start.")]
    [SerializeField] private TankRosterSO _roster;
    [Tooltip("Forces one tank, ignoring the roster and the player's choice. For testing.")]
    [SerializeField] private TankDefinitionSO _tankOverride;

    private TankDefinitionSO _tank;

    [Header("Input")]
    // WHY: left mouse is taken by the basic attack, so slots bind to keyboard keys.
    [SerializeField] private KeyCode _subKey = KeyCode.Q;
    [SerializeField] private KeyCode _exKey = KeyCode.E;
    [Tooltip("Held to fire the basic attack, matching how shooting already works.")]
    [SerializeField] private int _basicMouseButton = 0;

    [Header("Scene refs (found automatically when left empty)")]
    [SerializeField] private Transform _muzzle;
    [SerializeField] private Camera _camera;

    public SkillSlot Basic { get; private set; }
    public SkillSlot Sub { get; private set; }
    public SkillSlot EX { get; private set; }

    public event Action<TankSkillLoadout> LoadoutChanged;

    private SkillContext _ctx;
    private GameManager _manager;
    private Transform _behaviourRoot;

    public TankDefinitionSO Tank => _tank;

    private void Start()
    {
        // WHY: resolved before the context is built, because the tank supplies the baseline stats
        // that every skill's numbers are measured against.
        _tank = _tankOverride != null ? _tankOverride : TankSelection.Resolve(_roster);

        _manager = GameManager.Instance;
        if (_camera == null) _camera = _manager.mainCamera;
        if (_muzzle == null && _manager.shootPoint != null) _muzzle = _manager.shootPoint.transform;

        _behaviourRoot = new GameObject("SkillBehaviours").transform;
        _behaviourRoot.SetParent(transform, false);

        _ctx = new SkillContext(
            transform,
            _muzzle != null ? _muzzle : transform,
            _manager.playerStat,
            _tank != null && _tank.baseStats != null ? _tank.baseStats : _manager.playerBaseStat,
            GetComponent<PlayerManager>() ?? _manager.player.GetComponent<PlayerManager>(),
            _manager.projectilePool);

        Basic = new SkillSlot(SkillRole.Basic);
        Sub = new SkillSlot(SkillRole.Sub);
        EX = new SkillSlot(SkillRole.EX);

        if (_tank != null) ApplyTank(_tank);
    }

    public void ApplyTank(TankDefinitionSO tank)
    {
        _tank = tank;
        if (tank == null || _ctx == null) return;

        Equip(Basic, tank.basicSlot);
        Equip(Sub, tank.subSlot);
        Equip(EX, tank.exSlot);

        LoadoutChanged?.Invoke(this);
    }

    private void Equip(SkillSlot slot, SkillBinding binding)
    {
        if (binding == null) return;
        slot.Equip(binding.skill, binding.overrides, _ctx, _behaviourRoot);
    }

    private void Update()
    {
        if (_ctx == null) return;

        RefreshAim();

        float dt = Time.deltaTime;
        Basic.Tick(_ctx, dt);
        Sub.Tick(_ctx, dt);
        EX.Tick(_ctx, dt);

        DriveMouse(Basic, _basicMouseButton);
        DriveKey(Sub, _subKey);
        DriveKey(EX, _exKey);
    }

    // WHY: aim is read once per frame, in Update, and every skill reads the same value. Previously
    // each skill fetched its own cursor position, and one of them did it at the physics rate. When a
    // gamepad build happens, only this method changes.
    private void RefreshAim()
    {
        if (_camera == null) return;

        Vector3 raw = _camera.ScreenToWorldPoint(Input.mousePosition);
        _ctx.AimWorldPos = new Vector2(raw.x, raw.y);
    }

    private void DriveKey(SkillSlot slot, KeyCode key)
    {
        if (slot.IsEmpty || slot.IsPassive) return;

        switch (slot.Definition.activation)
        {
            case SkillActivation.Instant:
                if (Input.GetKeyDown(key)) slot.TryActivate(_ctx);
                break;

            case SkillActivation.Channeled:
                if (Input.GetKeyDown(key)) slot.TryActivate(_ctx);
                if (Input.GetKeyUp(key)) slot.Release(_ctx);
                break;

            case SkillActivation.Toggle:
                if (!Input.GetKeyDown(key)) break;
                if (slot.IsRunning) slot.Release(_ctx);
                else slot.TryActivate(_ctx);
                break;
        }
    }

    private void DriveMouse(SkillSlot slot, int button)
    {
        if (slot.IsEmpty || slot.IsPassive) return;

        switch (slot.Definition.activation)
        {
            case SkillActivation.Instant:
                // held, not pressed: the basic attack repeats on its own cooldown while the button is down
                if (Input.GetMouseButton(button)) slot.TryActivate(_ctx);
                break;

            case SkillActivation.Channeled:
                if (Input.GetMouseButtonDown(button)) slot.TryActivate(_ctx);
                if (Input.GetMouseButtonUp(button)) slot.Release(_ctx);
                break;

            case SkillActivation.Toggle:
                if (!Input.GetMouseButtonDown(button)) break;
                if (slot.IsRunning) slot.Release(_ctx);
                else slot.TryActivate(_ctx);
                break;
        }
    }

    private void OnDestroy()
    {
        if (_ctx == null) return;

        Basic?.Unequip(_ctx);
        Sub?.Unequip(_ctx);
        EX?.Unequip(_ctx);
    }
}
