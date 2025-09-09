using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private GridInventory gridInventory;

    [Header("Slots")]
    [SerializeField] private List<InventorySlot> gridSlots = new List<InventorySlot>();
    [SerializeField] private List<InventorySlot> spawnSlots = new List<InventorySlot>();

    [Header("Weapons")]
    [SerializeField] private List<WeaponSO> availableWeapons = new List<WeaponSO>();

    [Header("UI")]
    [SerializeField] private Button spawnButton;
    [SerializeField] private GameObject draggablePrefab;

    private void Awake()
    {
        // Auto-assign grid positions based on index
        for (int i = 0; i < gridSlots.Count; i++)
        {
            int x = i % gridInventory.width;
            int y = i / gridInventory.width;

            gridSlots[i].gridPosition = new Vector2Int(x, y);
            gridSlots[i].inventory = gridInventory;
        }

        // Link spawn slots as well
        foreach (var slot in spawnSlots)
        {
            slot.inventory = gridInventory;
        }
    }

    private void Start()
    {
        spawnButton.onClick.AddListener(SpawnWeapons);
        SpawnWeapons();
    }

    private void SpawnWeapons()
    {
        foreach (var slot in spawnSlots)
        {
            foreach (Transform child in slot.transform)
                Destroy(child.gameObject);

            WeaponSO randomWeapon = availableWeapons[Random.Range(0, availableWeapons.Count)];
            slot.SetWeapon(randomWeapon);

            if (randomWeapon != null)
            {
                var go = Instantiate(draggablePrefab, slot.transform);
                var drag = go.GetComponent<DraggableWeapon>();
                drag.Init(randomWeapon, slot);
            }
        }
    }
}
