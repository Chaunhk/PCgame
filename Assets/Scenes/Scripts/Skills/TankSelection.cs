using UnityEngine;

/// <summary>
/// Which tank the player picked. Survives scene loads and restarts.
/// </summary>
// WHY: the select screen does not exist yet, but the choice has to live somewhere that is not the
// gameplay scene — otherwise adding that screen later means rewiring how the tank reaches the
// player. A future screen only has to call Select(); nothing else changes.
public static class TankSelection
{
    private const string Key = "selected_tank_id";

    /// Empty until the player picks, which means "use the roster's default".
    public static string SelectedTankId
    {
        get { return PlayerPrefs.GetString(Key, string.Empty); }
    }

    public static void Select(TankDefinitionSO tank)
    {
        if (tank == null) return;

        PlayerPrefs.SetString(Key, tank.tankId);
        PlayerPrefs.Save();
    }

    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }

    /// <summary>The chosen tank, falling back to the roster's default when nothing is chosen.</summary>
    public static TankDefinitionSO Resolve(TankRosterSO roster)
    {
        if (roster == null) return null;

        TankDefinitionSO chosen = roster.Find(SelectedTankId);
        return chosen != null ? chosen : roster.Default;
    }
}
