using UnityEngine;

public class TetrominoSpawner : MonoBehaviour
{
    public GameSettingsSO settings;
    public TetrominoSO[] tetrominoCatalog;
    public GameObject tetrominoPrefab; // must have TetrominoInstance + DragController

    public Transform tetrominoParent;
    readonly System.Collections.Generic.List<TetrominoInstance> _rack = new System.Collections.Generic.List<TetrominoInstance>();

    public void ClearRack()
    {
        foreach (var t in _rack) if (t != null) Destroy(t.gameObject);
        _rack.Clear();
    }

    public void SpawnRack()
    {
        ClearRack();
        for (int i = 0; i < settings.rackCount; i++)
        {
            var so = tetrominoCatalog[Random.Range(0, tetrominoCatalog.Length)];
            var go = Instantiate(tetrominoPrefab, tetrominoParent);
            go.transform.position = new Vector3(settings.rackStart.x + i * settings.rackSpacing, settings.rackStart.y, 0);

            var ti = go.GetComponent<TetrominoInstance>();
            ti.Init(so, 1);

            var drag = go.GetComponent<DragController>();
            drag.settings = settings;
            drag.grid = FindObjectOfType<GridManager2D>();
            drag.SetDraggable(true);

            _rack.Add(ti);
        }
    }

    public void SetRackDraggable(bool enabled)
    {
        foreach (var t in _rack)
        {
            if (t == null) continue;
            var drag = t.GetComponent<DragController>();
            if (drag != null) drag.SetDraggable(enabled);
        }
    }
}