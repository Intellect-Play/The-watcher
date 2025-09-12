using UnityEngine;
using UnityEngine.UI;

public class StaticWeapon : MonoBehaviour
{
    public RandomWeaponSpawner randomWeaponSpawner;
    public SlotWeaponsSO weaponData;
    public Vector2Int gridPosition;
    public bool isActive;
    public int currentLevel=1;

    [SerializeField] private Image icon;
    [SerializeField] private Image icon2;

    [SerializeField] private PlacedWeapon? placedWeapon;

    private void Awake()
    {
        //placedWeapon = randomWeaponSpawner.inventoryManager.gridInventory.grid[gridPosition.x, gridPosition.y];
        //icon.GetComponent<Image>();

    }
    public void Init(SlotWeaponsSO _weaponData, int Level, Vector2Int pos)
    {
        gameObject.name = _weaponData.weaponName.ToString();
        weaponData = _weaponData;
        gridPosition = pos;
        isActive = false;
        SetIcon();

    }
    public void Shuffle(Vector2Int pos)
    {       
        gridPosition = pos;
    }
    public void Activate(PlacedWeapon[,] placedWeapons)
    {
        isActive = placedWeapons[gridPosition.x, gridPosition.y] != null;
        if(isActive)
        {
            placedWeapon = placedWeapons[gridPosition.x, gridPosition.y];
        }
        SetIcon();
    }
    public void LevelUp(int SpotWeaponLevel)
    {
        currentLevel = SpotWeaponLevel;
        SetIcon();


    }
    void SetIcon()
    {
        int iconNum = weaponData.levelToIconIndex[currentLevel - 1];
        icon2.sprite = weaponData.FadeOutIcons[iconNum];
        if (isActive)
        {
            icon.gameObject.SetActive(true);
            icon.sprite = weaponData.icons[iconNum];
        }
        else
        {
            icon.gameObject.SetActive(false);
        }
        //icon.sprite = isActive ? weaponData.icons[iconNum];

    }
}
