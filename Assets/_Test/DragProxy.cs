using UnityEngine;
using UnityEngine.EventSystems;

public class DragProxy : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private DraggableWeapon parent;

    private void Awake()
    {
        parent = GetComponentInParent<DraggableWeapon>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (parent == null) return;

        // pointerDrag-ı parent obyektə yönləndir
        eventData.pointerDrag = parent.gameObject;
        parent.OnBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (parent == null) return;

        eventData.pointerDrag = parent.gameObject;
        parent.OnDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (parent == null) return;

        eventData.pointerDrag = parent.gameObject;
        parent.OnEndDrag(eventData);
    }
}
