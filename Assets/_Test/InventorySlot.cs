using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    [HideInInspector] public Vector2Int gridPosition; // auto assigned by InventoryManager
    [HideInInspector] public GridInventory inventory;

    [SerializeField] private Image highlightImage; // optional, for icon or highlight

    // On drop we either place, merge, or reset dragged back to its original slot
    public void OnDrop(PointerEventData eventData)
    {
        var dragged = eventData.pointerDrag?.GetComponent<DraggableWeapon>();
        Debug.Log("OnDrop called on slot " + gridPosition + " with dragged " + (dragged != null ? dragged.weaponData.name : "null"));
        if (dragged == null || dragged.weaponData == null || inventory == null) return;

        WeaponSO weapon = dragged.weaponData;
        Vector2Int pos = gridPosition;
        Debug.Log("Dropping " + weapon.name + " at " + pos);
        // 1) Try merge (all shape cells are occupied by same weapon)
        if (weapon.nextLevelWeapon != null && inventory.CanMergeAt(weapon, pos, out List<PlacedWeapon> placedList))
        {
            Debug.Log("Merging " + weapon.name + " into " + weapon.nextLevelWeapon.name);
            // remove unique placed objects (unregister from grid + destroy their GameObjects)
            foreach (var p in placedList)
            {
                p.Unplace();
                Destroy(p.gameObject);
            }

            // destroy the dragged object (consumed in merge)
            Destroy(dragged.gameObject);

            // create merged placed weapon
            var mergedPlaced = inventory.CreatePlacedWeaponFromPrefab(inventory.placedPrefab, weapon.nextLevelWeapon, pos, this);
            // Update visual of slot if you use a highlight image
            if (highlightImage != null) highlightImage.sprite = mergedPlaced.weaponData.icon;

            return;
        }

        // 2) Try place into empty area
        if (inventory.CanPlace(weapon, pos))
        {
            // Move the dragged object into the slot and register it into the grid
            // If the dragged object is already a PlacedWeapon (picked from grid), we re-place it.
            var placedComp = dragged.GetComponent<PlacedWeapon>();
            if (placedComp != null)
            {
                // Place the same object (it was Unplaced at BeginDrag)
                dragged.transform.SetParent(transform);
                dragged.transform.localPosition = Vector3.zero;

                placedComp.weaponData = weapon;
                placedComp.Place(inventory, pos, this);

                // Set parentSlot so future drags know where to return if cancelled
                dragged.parentSlot = this;
            }
            else
            {
                // If no PlacedWeapon component exists (unlikely because prefab should have it),
                // create a placed object via inventory helper and destroy the dragged one.
                Destroy(dragged.gameObject);
                var newPlaced = inventory.CreatePlacedWeaponFromPrefab(inventory.placedPrefab, weapon, pos, this);
                if (highlightImage != null) highlightImage.sprite = newPlaced.weaponData.icon;
            }

            return;
        }

        // 3) Invalid placement — reset back to origin slot (spawn or previous)
        if (dragged.parentSlot != null)
        {
            dragged.transform.SetParent(dragged.parentSlot.transform);
            dragged.transform.localPosition = Vector3.zero;
        }
        else
        {
            // fallback: destroy or hide
            Destroy(dragged.gameObject);
        }
    }

    // Optional helper for showing icon on the slot background
    public void SetSlotIcon(Sprite icon)
    {
        if (highlightImage == null) return;
        highlightImage.sprite = icon;
        highlightImage.enabled = icon != null;
    }
}
