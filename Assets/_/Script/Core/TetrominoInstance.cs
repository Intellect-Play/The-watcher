using UnityEngine;
using System.Collections.Generic;

public class TetrominoInstance : MonoBehaviour
{
    public TetrominoSO Shape { get; private set; }
    public int Level { get; private set; } = 1; // increases on merge
    public bool IsPlaced { get; internal set; }

    readonly List<Transform> _cellVisuals = new List<Transform>();
    readonly List<GridCell> _occupiedCells = new List<GridCell>();

    public void Init(TetrominoSO shape, int level = 1)
    {
        Shape = shape;
        Level = level;
        BuildVisual();
    }

    void BuildVisual()
    {
        // Cleanup old visuals
        foreach (Transform t in transform) Destroy(t.gameObject);
        _cellVisuals.Clear();

        if (Shape.pieceVisualPrefab != null)
        {
            // Instantiate prefab as child
            var visGO = Instantiate(Shape.pieceVisualPrefab, transform);

            // Əgər prefab-ın özündə child yoxdursa, root-u da əlavə et,
            // əks halda bütün child-ları əlavə et
            var parent = visGO.transform;
            if (parent.childCount == 0)
            {
                _cellVisuals.Add(parent);
            }
            else
            {
                foreach (Transform c in parent)
                    _cellVisuals.Add(c);
            }
            return;
        }

        // Fallback: build simple quads if no prefab defined
        for (int y = 0; y < Shape.size.y; y++)
            for (int x = 0; x < Shape.size.x; x++)
            {
                if (!Shape.IsFilled(x, y)) continue;

                var go = new GameObject("cell_visual", typeof(SpriteRenderer), typeof(BoxCollider2D));
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.one * 0.9f;
                go.transform.localPosition = new Vector3(x, y, 0);

                var sr = go.GetComponent<SpriteRenderer>();
                // sr.sprite = someSprite; // əgər sprite qoşmaq istəsən
                var bc = go.GetComponent<BoxCollider2D>();
                bc.isTrigger = true;

                _cellVisuals.Add(go.transform);
            }
    }


    public bool CanPlaceAt(GridManager2D grid, Vector2Int origin)
    {
        _occupiedCells.Clear();
        foreach (var cell in grid.GetCoveredCells(origin, this))
        {
            if (cell == null) return false;
            _occupiedCells.Add(cell);
        }
        // If any covered cell is already occupied by a different piece, reject (unless full-match same type & level -> merge)
        foreach (var cell in _occupiedCells)
        {
            if (cell.OccupantPiece != null && cell.OccupantPiece != this)
            {
                // must check if potential merge
                if (!(cell.OccupantPiece.Shape.tetrominoId == Shape.tetrominoId && cell.OccupantPiece.Level == Level))
                    return false;
            }
        }
        return _occupiedCells.Count > 0; // valid if covers at least one cell
    }

    public bool TryPlace(GridManager2D grid, Vector2Int origin)
    {
        if (!CanPlaceAt(grid, origin)) return false;

        // Check if this exactly overlaps an existing piece of same type & level (for merge)
        bool isMerge = true;
        TetrominoInstance targetPiece = null;
        foreach (var cell in _occupiedCells)
        {
            if (cell.OccupantPiece == null) { isMerge = false; break; }
            if (targetPiece == null) targetPiece = cell.OccupantPiece;
            if (cell.OccupantPiece != targetPiece) { isMerge = false; break; }
        }
        if (isMerge && targetPiece != null)
        {
            if (targetPiece.Shape.tetrominoId == Shape.tetrominoId && targetPiece.Level == Level)
            {
                // Merge: bump level on the target, destroy this
                targetPiece.Level++;
                Destroy(gameObject);
                return true;
            }
            else
            {
                return false;
            }
        }

        // Normal placement: occupy cells & activate weapons
        foreach (var cell in _occupiedCells)
        {
            cell.OccupantPiece = this;
            cell.Weapon?.AddActivator();
        }
        IsPlaced = true;
        return true;
    }

    public void Unplace()
    {
        if (!IsPlaced) return;
        foreach (Transform t in transform) { }
        // Deactivate weapons for previously occupied cells
        var grid = FindObjectOfType<GridManager2D>();
        if (grid != null)
        {
            for (int y = 0; y < grid.Cells.GetLength(1); y++)
                for (int x = 0; x < grid.Cells.GetLength(0); x++)
                {
                    var cell = grid.Cells[x, y];
                    if (cell.OccupantPiece == this)
                    {
                        cell.OccupantPiece = null;
                        cell.Weapon?.RemoveActivator();
                    }
                }
        }
        IsPlaced = false;
    }
}
