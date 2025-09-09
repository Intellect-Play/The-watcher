using UnityEngine;
using System.Collections.Generic;

public class GridManager2D : MonoBehaviour
{
    public GameSettingsSO settings;
    public GameObject gridCellPrefab; // must have GridCell + BoxCollider2D
    public GameObject weaponTilePrefab; // must have WeaponTile
    public Transform gridParent;
    public Transform weaponParent;
    public GridCell[,] Cells { get; private set; }

    readonly List<WeaponTile> _weapons = new List<WeaponTile>();

    void Awake()
    {
        BuildGrid();
        SpawnRandomWeapons();
    }

    public Vector3 CellToWorld(Vector2Int c)
    {
        return new Vector3(settings.gridOrigin.x + c.x * settings.cellSize + settings.cellSize * 0.5f,
                           settings.gridOrigin.y + c.y * settings.cellSize + settings.cellSize * 0.5f, 0f);
    }

    public bool InBounds(Vector2Int c)
    {
        return c.x >= 0 && c.y >= 0 && c.x < settings.gridCols && c.y < settings.gridRows;
    }

    void BuildGrid()
    {
        Cells = new GridCell[settings.gridCols, settings.gridRows];
        for (int y = 0; y < settings.gridRows; y++)
            for (int x = 0; x < settings.gridCols; x++)
            {
                var go = Instantiate(gridCellPrefab, CellToWorld(new Vector2Int(x, y)), Quaternion.identity, gridParent);
                go.name = $"Cell_{x}_{y}";
                var cell = go.GetComponent<GridCell>();
                cell.Init(new Vector2Int(x, y));
                Cells[x, y] = cell;
            }
    }

    void SpawnRandomWeapons()
    {
        // Example: spawn a weapon tile in every cell initially (you can randomize or sparse)
        for (int y = 0; y < settings.gridRows; y++)
            for (int x = 0; x < settings.gridCols; x++)
            {
                var wgo = Instantiate(weaponTilePrefab, Cells[x, y].transform.position, Quaternion.identity, weaponParent);
                var weapon = wgo.GetComponent<WeaponTile>();
                weapon.RandomizeSO();
                Cells[x, y].SetWeapon(weapon);
                _weapons.Add(weapon);
            }
    }

    public IEnumerable<GridCell> GetCoveredCells(Vector2Int origin, TetrominoInstance piece)
    {
        for (int y = 0; y < piece.Shape.size.y; y++)
            for (int x = 0; x < piece.Shape.size.x; x++)
            {
                if (!piece.Shape.IsFilled(x, y)) continue;
                var c = origin + new Vector2Int(x, y);
                if (!InBounds(c)) yield break; // out of bounds -> invalid
                yield return Cells[c.x, c.y];
            }
    }
}
