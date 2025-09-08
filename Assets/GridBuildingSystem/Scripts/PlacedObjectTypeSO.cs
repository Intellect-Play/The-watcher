using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Placed Object Type")]
public class PlacedObjectTypeSO : ScriptableObject
{

    public enum Dir
    {
        Down,
        Left,
        Up,
        Right,
    }

    public enum ShapeType
    {
        Rectangle,
        T,
        L,
        Z,
        Custom
    }

    public string nameString;
    public Transform prefab;
    public Transform visual;

    [Header("Shape Settings")]
    public ShapeType shapeType = ShapeType.Rectangle;
    public int width;
    public int height;
    public bool firstBuild = false;

    [Header("Manual Sizes (if firstBuild = true)")]
    public Vector2Int size3;
    public Vector2Int size2;
    public Vector2Int size1;

    [Header("Custom Shape (local grid positions)")]
    public List<Vector2Int> customShape = new List<Vector2Int>();


    // ---------------- Rotation & Direction ----------------
    public static Dir GetNextDir(Dir dir)
    {
        switch (dir)
        {
            default:
            case Dir.Down: return Dir.Left;
            case Dir.Left: return Dir.Up;
            case Dir.Up: return Dir.Right;
            case Dir.Right: return Dir.Down;
        }
    }

    public int GetRotationAngle(Dir dir)
    {
        switch (dir)
        {
            default:
            case Dir.Down: return 0;
            case Dir.Left: return 90;
            case Dir.Up: return 180;
            case Dir.Right: return 270;
        }
    }

    public Vector2Int GetRotationOffset(Dir dir)
    {
        switch (dir)
        {
            default:
            case Dir.Down: return new Vector2Int(0, 0);
            case Dir.Left: return new Vector2Int(0, width);
            case Dir.Up: return new Vector2Int(width, height);
            case Dir.Right: return new Vector2Int(height, 0);
        }
    }


    // ---------------- Grid Shape ----------------
    public List<Vector2Int> GetGridPositionList(Vector2Int offset, Dir dir)
    {
        List<Vector2Int> gridPositionList = new List<Vector2Int>();

        switch (shapeType)
        {
            // Default rectangle
            case ShapeType.Rectangle:
                switch (dir)
                {
                    default:
                    case Dir.Down:
                    case Dir.Up:
                        if (firstBuild)
                        {
                            gridPositionList.Add(offset + size1);
                            gridPositionList.Add(offset + size2);
                            gridPositionList.Add(offset + size3);
                            Debug.Log("Size1: " + size1 + " Size2: " + size2 + " Size3: " + size3);
                        }
                        else
                        {
                            for (int x = 0; x < width; x++)
                            {
                                for (int y = 0; y < height; y++)
                                {
                                    gridPositionList.Add(offset + new Vector2Int(x, y));
                                }
                            }
                        }
                        break;

                    case Dir.Left:
                    case Dir.Right:
                        for (int x = 0; x < height; x++)
                        {
                            for (int y = 0; y < width; y++)
                            {
                                gridPositionList.Add(offset + new Vector2Int(x, y));
                            }
                        }
                        break;
                }
                break;

            // T shape
            case ShapeType.T:
                gridPositionList.Add(offset + new Vector2Int(0, 0));
                gridPositionList.Add(offset + new Vector2Int(1, 0));
                gridPositionList.Add(offset + new Vector2Int(2, 0));
                gridPositionList.Add(offset + new Vector2Int(1, 1));
                break;

            // L shape
            case ShapeType.L:
                gridPositionList.Add(offset + new Vector2Int(0, 0));
                gridPositionList.Add(offset + new Vector2Int(0, 1));
                gridPositionList.Add(offset + new Vector2Int(0, 2));
                gridPositionList.Add(offset + new Vector2Int(1, 0));
                break;

            // Z shape
            case ShapeType.Z:
                gridPositionList.Add(offset + new Vector2Int(0, 0));
                gridPositionList.Add(offset + new Vector2Int(1, 0));
                gridPositionList.Add(offset + new Vector2Int(1, 1));
                gridPositionList.Add(offset + new Vector2Int(2, 1));
                break;

            // Custom inspector-defined shape
            case ShapeType.Custom:
                foreach (Vector2Int pos in customShape)
                {
                    gridPositionList.Add(offset + pos);
                }
                break;
        }

        return gridPositionList;
    }
}
