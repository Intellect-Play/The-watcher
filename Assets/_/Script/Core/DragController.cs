using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TetrominoInstance))]
public class DragController : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameSettingsSO settings;
    public GridManager2D grid;

    Vector3 _dragOffset;
    Vector3 _startPos;
    bool _canDrag = true;

    TetrominoInstance _piece;

    void Awake()
    {
        _piece = GetComponent<TetrominoInstance>();
    }

    public void SetDraggable(bool canDrag)
    {
        _canDrag = canDrag;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_canDrag) return;
        _startPos = transform.position;
        _piece.Unplace();
        _dragOffset = transform.position - ScreenToWorld(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_canDrag) return;
        transform.position = ScreenToWorld(eventData.position) + _dragOffset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_canDrag) return;

        // Snap to nearest grid origin (bottom-left of tetromino footprint)
        Vector3 world = transform.position;
        Vector2Int origin = WorldToCellOrigin(world);
        if (_piece.TryPlace(grid, origin))
        {
            // Snap transform to exact position
            transform.position = grid.CellToWorld(origin) + new Vector3(0, 0, 0);
        }
        else
        {
            // Return to rack/original
            transform.position = _startPos;
        }
    }

    Vector3 ScreenToWorld(Vector2 screen)
    {
        var cam = Camera.main;
        var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
        w.z = 0f;
        return w;
    }

    Vector2Int WorldToCellOrigin(Vector3 world)
    {
        Vector2 local = new Vector2(world.x - settings.gridOrigin.x, world.y - settings.gridOrigin.y);
        int cx = Mathf.RoundToInt(local.x / settings.cellSize - 0.5f);
        int cy = Mathf.RoundToInt(local.y / settings.cellSize - 0.5f);
        return new Vector2Int(cx, cy);
    }
}