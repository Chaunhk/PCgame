using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Binds one HUD element to one skill slot: icon, cooldown sweep, charge count.
/// </summary>
// WHY: SkillBar used to be driven by whichever skill script happened to hold a reference to it, and
// its cooldown length came from a private field that InitData never set — so every skill's cooldown
// was silently the hardcoded 5s default. The slot is the only thing that knows the real number now,
// and the view subscribes to it instead of being poked by the skill.
public class SkillSlotView : MonoBehaviour
{
    public enum Slot { Basic, Sub, EX }

    [SerializeField] private Slot _slot = Slot.Sub;
    [SerializeField] private TankSkillLoadout _loadout;

    [Header("Widgets (all optional)")]
    [SerializeField] private Image _icon;
    [Tooltip("Radial or filled image swept from 1 to 0 while recovering.")]
    [SerializeField] private Image _cooldownFill;
    [SerializeField] private Text _chargeLabel;
    [SerializeField] private GameObject _emptyState;

    private SkillSlot _bound;

    private void Start()
    {
        if (_loadout == null) _loadout = FindFirstObjectByType<TankSkillLoadout>();
        if (_loadout == null) return;

        _loadout.LoadoutChanged += OnLoadoutChanged;
        Bind();
    }

    private void OnDestroy()
    {
        if (_loadout != null) _loadout.LoadoutChanged -= OnLoadoutChanged;
        if (_bound != null) _bound.Changed -= Refresh;
    }

    private void OnLoadoutChanged(TankSkillLoadout loadout)
    {
        Bind();
    }

    private void Bind()
    {
        if (_bound != null) _bound.Changed -= Refresh;

        switch (_slot)
        {
            case Slot.Basic: _bound = _loadout.Basic; break;
            case Slot.Sub: _bound = _loadout.Sub; break;
            default: _bound = _loadout.EX; break;
        }

        if (_bound != null) _bound.Changed += Refresh;
        Refresh(_bound);
    }

    private void Update()
    {
        // the cooldown sweep is the one thing that changes every frame; everything else is event-driven
        if (_bound == null || _cooldownFill == null || _bound.IsEmpty || _bound.Values == null) return;

        float cooldown = _bound.Values.Cooldown;
        _cooldownFill.fillAmount = cooldown > 0f ? Mathf.Clamp01(_bound.CooldownRemaining / cooldown) : 0f;
    }

    private void Refresh(SkillSlot slot)
    {
        bool empty = slot == null || slot.IsEmpty;

        if (_emptyState != null) _emptyState.SetActive(empty);
        if (_icon != null)
        {
            _icon.enabled = !empty && slot.Definition.icon != null;
            if (!empty) _icon.sprite = slot.Definition.icon;
        }

        if (_chargeLabel != null)
        {
            // a one-charge skill shows nothing — a "1" next to every icon is noise
            bool showCharges = !empty && !slot.IsPassive && slot.Values != null && slot.Values.MaxCharges > 1;
            _chargeLabel.gameObject.SetActive(showCharges);
            if (showCharges) _chargeLabel.text = slot.ChargesLeft.ToString();
        }

        if (_cooldownFill != null) _cooldownFill.enabled = !empty && !slot.IsPassive;
    }
}
