using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

[CreateAssetMenu(menuName = "Game/Settings", fileName = "GameSettings")]
public class GameSettingsSO : ScriptableObject
{
    [Header("Grid")]
    [Min(1)] public int gridRows = 5;
    [Min(1)] public int gridCols = 5;
    public float cellSize = 1f;
    public Vector2 gridOrigin = new Vector2(0, 0); // world position of bottom-left cell

    [Header("Tetromino Rack")]
    public int rackCount = 3;
    public Vector2 rackStart = new Vector2(0, -3f); // where to place first rack piece
    public float rackSpacing = 2.2f;

    [Header("Gameplay")]
    public bool dragEnabledAtStart = true;
    public LayerMask gridCellLayer;
    public LayerMask tetrominoCellLayer;
}