using UnityEngine;

[CreateAssetMenu(menuName = "Game/Tetromino", fileName = "Tetromino")]
public class TetrominoSO : ScriptableObject
{
    public string tetrominoId = "I"; // unique key for merge check
    [Tooltip("2D shape footprint. true = filled cell.")]
    public Vector2Int size = new Vector2Int(2, 2);
    public bool[] cells; // flattened size.x * size.y grid

    [Header("Spawn")]
    public GameObject pieceVisualPrefab; // for rendering the piece (children per cell recommended)

    public bool IsFilled(int x, int y)
    {
        if (x < 0 || y < 0 || x >= size.x || y >= size.y) return false;
        int idx = y * size.x + x;
        if (idx < 0 || idx >= cells.Length) return false;
        return cells[idx];
    }
}