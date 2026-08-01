using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Every tank the player can pick, in the order they should be offered.
/// </summary>
// WHY: without a roster asset, "which tanks exist" would be whatever happens to be assigned in the
// scene, and the selection screen that does not exist yet would have nothing to enumerate. One
// asset means the designer window, the future select screen and the game all read the same list.
[CreateAssetMenu(fileName = "TankRoster", menuName = "ScriptableObjects/Tank Roster")]
public class TankRosterSO : ScriptableObject
{
    public List<TankDefinitionSO> tanks = new List<TankDefinitionSO>();

    [Tooltip("Used when the player has not chosen yet. Empty means the first entry.")]
    public string defaultTankId;

    public TankDefinitionSO Find(string tankId)
    {
        if (string.IsNullOrEmpty(tankId)) return null;

        for (int i = 0; i < tanks.Count; i++)
            if (tanks[i] != null && tanks[i].tankId == tankId) return tanks[i];

        return null;
    }

    public TankDefinitionSO Default
    {
        get
        {
            TankDefinitionSO byId = Find(defaultTankId);
            if (byId != null) return byId;

            for (int i = 0; i < tanks.Count; i++)
                if (tanks[i] != null) return tanks[i];

            return null;
        }
    }
}
