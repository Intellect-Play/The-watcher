using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private GridInventory gridInventory;

    [Header("Slots")]
    [Tooltip("Grid slots in row-major order (x=0..width-1, y=0..height-1)")]
    [SerializeField] private List<InventorySlot> gridSlots = new List<InventorySlot>();

    [SerializeField] private List<InventorySlot> spawnSlots = new List<InventorySlot>();

    [Header("Weapons")]
    [SerializeField] private List<GameObject> availableWeapons = new List<GameObject>();

    [Header("UI")]
    [SerializeField] private Button spawnButton;
    [SerializeField] private GameObject draggablePrefab; // prefab that contains Image + CanvasGroup + DraggableWeapon + PlacedWeapon

    private void Awake()
    {
        if (gridInventory == null)
        {
            Debug.LogError("GridInventory reference required.");
            return;
        }

        // tell gridInventory which prefab to use for placed items
        gridInventory.placedPrefab = draggablePrefab;

        // auto-assign grid positions
        for (int i = 0; i < gridSlots.Count; i++)
        {
            int x = i % gridInventory.width;
            int y = i / gridInventory.width;
            gridSlots[i].gridPosition = new Vector2Int(x, y);
            gridSlots[i].inventory = gridInventory;
        }

        // ensure spawn slots know inventory reference (useful for reset)
        foreach (var s in spawnSlots) s.inventory = gridInventory;
    }

    private void Start()
    {
        if (spawnButton != null) spawnButton.onClick.AddListener(SpawnWeapons);
        SpawnWeapons();
    }

    public void SpawnWeapons()
    {
        foreach (var slot in spawnSlots)
        {
            // Clear existing children
            foreach (Transform c in slot.transform) Destroy(c.gameObject);

            // pick random
            GameObject randomWeapon = availableWeapons[Random.Range(0, availableWeapons.Count)];

            // instantiate draggable prefab under the spawn slot
            var go = Instantiate(randomWeapon, slot.transform);
            go.transform.localPosition = Vector3.zero;

            var drag = go.GetComponent<DraggableWeapon>();
            var placed = go.GetComponent<PlacedWeapon>();

            if (drag == null || placed == null)
            {
                Debug.LogError("draggablePrefab must have DraggableWeapon and PlacedWeapon components.");
                Destroy(go);
                continue;
            }
            WeaponSO weaponData = randomWeapon.GetComponent<DraggableWeapon>()?.weaponData;
            // init both: this object is a spawn copy (not placed)
            drag.Init(weaponData, slot);
            placed.InitAsSpawn(weaponData);

            // optionally set slot icon
            slot.SetSlotIcon(randomWeapon != null ? weaponData.icon : null);
        }
    }
}
