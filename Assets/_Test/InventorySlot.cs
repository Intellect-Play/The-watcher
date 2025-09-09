using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    public Vector2Int gridPosition; // set in inspector or dynamically
    public GridInventory inventory;
    [SerializeField] private Image highlightImage;

    private WeaponSO currentWeapon;

    public void SetWeapon(WeaponSO weapon)
    {
        currentWeapon = weapon;
        if (highlightImage != null)
        {
            highlightImage.sprite = weapon != null ? weapon.icon : null;
            highlightImage.enabled = weapon != null;
        }
    }

    public WeaponSO GetWeapon() => currentWeapon;

    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag?.GetComponent<DraggableWeapon>();
        if (dragged == null || dragged.weaponData == null) return;

        WeaponSO weapon = dragged.weaponData;

        // Try merge first
        if (currentWeapon == weapon && currentWeapon.nextLevelWeapon != null)
        {
            SetWeapon(currentWeapon.nextLevelWeapon);
            inventory.Clear(currentWeapon);
            inventory.Place(currentWeapon.nextLevelWeapon, gridPosition);
            Destroy(dragged.gameObject);
            return;
        }

        // Try place in empty area
        if (currentWeapon == null && inventory.CanPlace(weapon, gridPosition))
        {
            SetWeapon(weapon);
            inventory.Place(weapon, gridPosition);

            dragged.transform.SetParent(transform);
            dragged.transform.localPosition = Vector3.zero;
            return;
        }

        // If invalid, reset to original
        dragged.transform.SetParent(dragged.parentSlot.transform);
        dragged.transform.localPosition = Vector3.zero;
    }
}
