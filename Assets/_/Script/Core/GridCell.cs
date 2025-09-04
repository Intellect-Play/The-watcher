using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class GridCell : MonoBehaviour
{
    public Vector2Int Coord { get; private set; }
    public WeaponTile Weapon { get; private set; }
    public TetrominoInstance OccupantPiece { get; internal set; } // which piece currently covers this cell

    BoxCollider2D _col;

    public void Init(Vector2Int coord)
    {
        Coord = coord;
        _col = GetComponent<BoxCollider2D>();
        _col.isTrigger = true; // we use Overlap detection
    }

    public void SetWeapon(WeaponTile weapon)
    {
        Weapon = weapon;
        if (weapon != null) weapon.AttachToCell(this);
    }
}
