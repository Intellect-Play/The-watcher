using UnityEngine;

[CreateAssetMenu(menuName = "Game/Weapon", fileName = "Weapon")]
public class WeaponSO : ScriptableObject
{
    public string weaponId = "Pistol";
    public GameObject projectilePrefab; // optional
    [Min(0.01f)] public float attackInterval = 1f;
    public float damage = 5f;
    public float range = 8f;

    [Header("Visuals")]
    public Sprite sprite;
}
