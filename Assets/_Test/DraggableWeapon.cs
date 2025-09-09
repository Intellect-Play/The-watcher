using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class DraggableWeapon : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public WeaponSO weaponData;
    [HideInInspector] public InventorySlot parentSlot;

    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private Canvas canvas;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void Init(WeaponSO weapon, InventorySlot slot)
    {
        weaponData = weapon;
        parentSlot = slot;

        var img = GetComponent<Image>();
        if (img != null)
        {
            img.sprite = weapon != null ? weapon.icon : null;
            img.enabled = weapon != null;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (weaponData == null) return;

        originalParent = transform.parent;
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
        transform.SetParent(originalParent);
        canvasGroup.blocksRaycasts = true;
    }
}
