using UnityEngine;

[RequireComponent(typeof(DraggableWeapon))]
public class PlacedWeapon : MonoBehaviour
{
    [HideInInspector] public WeaponSO weaponData;
    [HideInInspector] public Vector2Int origin = new Vector2Int(-1, -1);

    private GridInventory inventory;
    private InventorySlot originSlot;

    public bool IsPlaced => origin.x >= 0 && inventory != null;

    // Call when prefab is used as a spawned (not yet placed) object
    public void InitAsSpawn(WeaponSO weapon)
    {
        weaponData = weapon;
        origin = new Vector2Int(-1, -1);
        inventory = null;
        originSlot = null;
    }

    // Register into grid at pos and remember origin UI slot
    public void Place(GridInventory grid, Vector2Int pos, InventorySlot slot)
    {
        inventory = grid;
        origin = pos;
        originSlot = slot;
        grid.PlacePlacedWeapon(this, pos);
    }

    // Unregister from grid but do not destroy GameObject (used when player picks up)
    public void Unplace()
    {
        if (inventory != null)
        {
            inventory.RemovePlacedWeapon(this);
            inventory = null;
        }
        origin = new Vector2Int(-1, -1);
        originSlot = null;
    }
}
