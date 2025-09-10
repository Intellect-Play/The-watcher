using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableWeapon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public WeaponSO weaponData;
    public InventorySlot parentSlot; // current slot (spawn slot or grid's origin slot)

    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private InventorySlot originalParentSlot;
    private Canvas canvas;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    // Initialize sprite and slot reference (used both for spawn and when creating placed item)
    public void Init(WeaponSO weapon, InventorySlot slot)
    {
        weaponData = weapon;
        parentSlot = slot;

        var img = GetComponent<Image>();
        if (img != null)
        {
            img.sprite = weapon != null ? weapon.icon : null;
            img.enabled = weapon != null;
            //img.SetNativeSize();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (weaponData == null) return;

        originalParent = transform.parent;
        originalParentSlot = parentSlot;

        // If this GameObject is a placed object, unplace it so cells become free while dragging
        var placed = GetComponent<PlacedWeapon>();
        if (placed != null && placed.IsPlaced)
        {
            placed.Unplace();
        }

        // Move to top-level canvas while dragging (so it renders over everything)
        if (canvas != null)
            transform.SetParent(canvas.transform);

        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (weaponData == null) return;
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // OnDrop (if successful) will change parent; if still parented to canvas, return to original
        if (transform.parent == canvas.transform)
        {
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero;
            parentSlot = originalParentSlot;
        }

        canvasGroup.blocksRaycasts = true;
    }
}
