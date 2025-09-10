using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GridInventory : MonoBehaviour
{
    public int width = 5;
    public int height = 5;

    // cell -> PlacedWeapon occupying it (or null)
    private PlacedWeapon[,] grid;

    // prefab used for creating placed result (set by InventoryManager on Awake)
    [HideInInspector] public GameObject placedPrefab;

    private void Awake()
    {
        grid = new PlacedWeapon[width, height];
    }

    public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < width && y < height;

    // check if area for weapon is fully free (no PlacedWeapon)
    public bool CanPlace(WeaponSO weapon, Vector2Int pos)
    {
        foreach (var offset in weapon.shapeOffsets)
        {
            int x = pos.x + offset.x;
            int y = pos.y + offset.y;
            if (!InBounds(x, y)) return false;
            if (grid[x, y] != null) return false;
        }
        return true;
    }

    // Place the PlacedWeapon reference into all cells described by its weaponData
    public void PlacePlacedWeapon(PlacedWeapon placed, Vector2Int pos)
    {
        foreach (var offset in placed.weaponData.shapeOffsets)
        {
            int x = pos.x + offset.x;
            int y = pos.y + offset.y;
            grid[x, y] = placed;
        }
    }

    // Remove references to this PlacedWeapon in grid cells
    public void RemovePlacedWeapon(PlacedWeapon placed)
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (grid[x, y] == placed) grid[x, y] = null;
    }

    public PlacedWeapon GetPlacedAt(int x, int y)
    {
        if (!InBounds(x, y)) return null;
        return grid[x, y];
    }

    // Returns true & list of placed objects covering the offsets if each cell contains the same weapon type
    public bool CanMergeAt(WeaponSO weapon, Vector2Int pos, out List<PlacedWeapon> placedOut)
    {
        placedOut = new List<PlacedWeapon>();
        foreach (var offset in weapon.shapeOffsets)
        {
            int x = pos.x + offset.x;
            int y = pos.y + offset.y;
            if (!InBounds(x, y)) return false;
            var p = grid[x, y];
            if (p == null || p.weaponData != weapon) return false;
            placedOut.Add(p);
        }
        // deduplicate placed instances
        placedOut = placedOut.Distinct().ToList();
        return true;
    }

    // Helper to create a placed weapon (instantiates the provided prefab under slot transform).
    // Prefab must contain DraggableWeapon + PlacedWeapon
    public PlacedWeapon CreatePlacedWeaponFromPrefab(GameObject prefab, WeaponSO weapon, Vector2Int pos, InventorySlot slot)
    {
        var go = Instantiate(prefab, slot.transform);
        go.transform.localPosition = Vector3.zero;

        var draggable = go.GetComponent<DraggableWeapon>();
        var placed = go.GetComponent<PlacedWeapon>();

        if (draggable == null || placed == null)
        {
            Debug.LogError("Prefab must contain DraggableWeapon and PlacedWeapon components.");
            Destroy(go);
            return null;
        }

        // initialize visuals and state
        draggable.Init(weapon, slot);
        placed.weaponData = weapon;
        placed.Place(this, pos, slot);
        return placed;
    }
}
