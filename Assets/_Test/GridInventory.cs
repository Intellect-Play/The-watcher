using UnityEngine;

public class GridInventory : MonoBehaviour
{
    public int width = 5;
    public int height = 5;
    private WeaponSO[,] grid;

    private void Awake()
    {
        grid = new WeaponSO[width, height];
    }

    public bool CanPlace(WeaponSO weapon, Vector2Int pos)
    {
        foreach (var offset in weapon.shapeOffsets)
        {
            int x = pos.x + offset.x;
            int y = pos.y + offset.y;

            if (x < 0 || y < 0 || x >= width || y >= height) return false;
            if (grid[x, y] != null) return false; // already occupied
        }
        return true;
    }

    public void Place(WeaponSO weapon, Vector2Int pos)
    {
        foreach (var offset in weapon.shapeOffsets)
        {
            int x = pos.x + offset.x;
            int y = pos.y + offset.y;
            grid[x, y] = weapon;
        }
    }

    public void Clear(WeaponSO weapon)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == weapon)
                    grid[x, y] = null;
            }
        }
    }

    public bool TryMerge(WeaponSO weapon, Vector2Int pos, out WeaponSO mergedWeapon)
    {
        foreach (var offset in weapon.shapeOffsets)
        {
            int x = pos.x + offset.x;
            int y = pos.y + offset.y;

            if (grid[x, y] != weapon)
            {
                mergedWeapon = null;
                return false;
            }
        }

        if (weapon.nextLevelWeapon != null)
        {
            Place(weapon.nextLevelWeapon, pos);
            mergedWeapon = weapon.nextLevelWeapon;
            return true;
        }

        mergedWeapon = null;
        return false;
    }
}
