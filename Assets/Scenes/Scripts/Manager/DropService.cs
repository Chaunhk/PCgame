using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropService : MonoBehaviour
{
    public void SpawnDrops(DropTable table, Vector3 position)
    {
        foreach (var drop in table.drops)
        {
            if (Random.value <= drop.dropChance)
            {
                Instantiate(drop.prefab, position, Quaternion.identity);
            }
        }
    }
}
