using UnityEngine;

/// <summary>
/// Everything a skill behaviour is allowed to know, handed to it rather than fetched.
/// </summary>
// WHY: behaviours reaching into GameManager.Instance for the player, the camera and the stats is
// what made the two existing skills impossible to test or reuse. Passing the context in means a
// behaviour has no opinion about where the tank came from — and when a gamepad build changes how
// aim is produced, only the code that fills AimWorldPos changes.
public class SkillContext
{
    /// Tank root.
    public Transform Owner { get; private set; }
    /// Where projectiles leave from.
    public Transform Muzzle { get; private set; }
    /// Cursor position in world space, refreshed once per frame by the loadout.
    public Vector2 AimWorldPos { get; set; }
    public PlayerStatSO Stats { get; private set; }
    /// The tank's starting stats. Global modifiers count as their change from this, not their value.
    public PlayerStatSO BaseStats { get; private set; }
    public PlayerManager Player { get; private set; }
    public ProjectilePool Pool { get; private set; }
    /// Resolved numbers for the skill being run. Set by the slot before every call.
    public SkillRuntimeValues Values { get; set; }

    public SkillContext(Transform owner, Transform muzzle, PlayerStatSO stats, PlayerStatSO baseStats, PlayerManager player, ProjectilePool pool)
    {
        Owner = owner;
        Muzzle = muzzle;
        Stats = stats;
        BaseStats = baseStats;
        Player = player;
        Pool = pool;
    }

    /// Aim direction from the tank, normalised. Falls back to the tank's facing when the cursor
    /// sits exactly on it, so a skill never fires along a zero vector.
    public Vector2 AimDirection
    {
        get
        {
            Vector2 delta = AimWorldPos - (Vector2)Owner.position;
            return delta.sqrMagnitude > 0.0001f ? delta.normalized : (Vector2)Owner.up;
        }
    }

    public Quaternion AimRotation
    {
        get
        {
            Vector2 dir = AimDirection;
            return Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        }
    }
}
