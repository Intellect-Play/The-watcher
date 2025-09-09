using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu()]

public class MainGameSO : ScriptableObject
{
    [Header("Main Grid Settings")]
    public int gridWidth = 4;
    public int gridHeight = 4;
    public float cellSize = 100;

   
}
