// ===============================
// Project Structure (suggested)
// ===============================
// Scripts/
//   SO/
//     GameSettingsSO.cs
//     WeaponSO.cs
//     TetrominoSO.cs
//   Core/
//     GridManager2D.cs
//     GridCell.cs
//     WeaponTile.cs
//     TetrominoInstance.cs
//     TetrominoSpawner.cs
//     DragController.cs
//     BattleController.cs
//     WaveManager.cs
//     ObjectPool.cs (optional simple pool)
//
// Notes:
// - 2D game. Use BoxCollider2D on GridCell and each Tetromino cell part for overlap detection.
// - Key tunables live in ScriptableObjects (SO).
// - Grid size is configurable (default 5x5).
// - 3 tetrominoes spawn under the grid (rack). Drag & drop onto grid.
// - When battle starts: disabling drag, any leftover rack pieces are destroyed.
// - Weapons sitting on the grid become ACTIVE under placed tetromino cells and attack every WeaponSO.AttackInterval seconds.
// - Merge: placing a tetromino exactly over same-shape & same-level tetromino merges -> level++.
// - Wave end: call WaveManager.EndWave() or BattleController.OnWaveEnded() to spawn new 3 tetrominoes and re-enable drag.
// - Everything kept SOLID-ish and clean; events used for state changes.

// ===============================
// SO/GameSettingsSO.cs
// ===============================


// ===============================
// SO/WeaponSO.cs
// ===============================

// ===============================
// SO/TetrominoSO.cs
// ===============================


// ===============================
// Core/GridCell.cs
// ===============================

// ===============================
// Core/GridManager2D.cs
// ===============================

// ===============================
// Core/WeaponTile.cs
// ===============================
using UnityEngine;
using System;

public class WeaponTile : MonoBehaviour
{
    public WeaponSO weaponData; // can be randomized if null

    public bool IsActive => _activeCount > 0;
    int _activeCount = 0; // number of tetromino cells covering this weapon

    float _timer;

    GridCell _cell;

    public event Action<WeaponTile> OnAttackTick;

    void Update()
    {
        if (!IsActive || weaponData == null) return;
        _timer -= Time.deltaTime;
        if (_timer <= 0f)
        {
            _timer = weaponData.attackInterval;
            Attack();
        }
    }

    public void AttachToCell(GridCell cell)
    {
        _cell = cell;
    }

    public void RandomizeSO()
    {
        // Placeholder: keep whatever assigned. Implement your random pick from a WeaponSO list via a manager.
    }

    void Attack()
    {
        // TODO: Hook your projectile/zombie system here.
        // Example minimal behavior:
        Debug.Log($"[WeaponTile] {weaponData?.weaponId} @ {_cell?.Coord} attacks for {weaponData?.damage}");
        OnAttackTick?.Invoke(this);
    }

    public void AddActivator()
    {
        _activeCount++;
        if (_activeCount == 1)
        {
            _timer = 0f; // fire immediately on activation
        }
    }

    public void RemoveActivator()
    {
        _activeCount = Mathf.Max(0, _activeCount - 1);
    }
}

// ===============================
// Core/TetrominoInstance.cs
// ===============================

// ===============================
// Core/DragController.cs
// ===============================


// ===============================
// Core/TetrominoSpawner.cs
// ===============================


// ===============================
// Core/BattleController.cs
// ===============================

// ===============================
// Core/WaveManager.cs
// ===============================

// ===============================
// Core/ObjectPool.cs (optional – stub)
// ===============================

// ===============================
// Minimal Setup Instructions (Inspector)
// ===============================
// 1) Create SOs:
//    - GameSettingsSO: gridRows=5, gridCols=5, cellSize=1, gridOrigin=(0,0), rackCount=3.
//    - A few WeaponSO assets (e.g., Pistol, Shotgun) and assign sprites/intervals.
//    - A few TetrominoSO assets: define size & cells (e.g., classic T, L, I, O, S), set tetrominoId to unique ids.
// 2) Scene objects:
//    - GridManager2D: assign GameSettingsSO, gridCellPrefab (with GridCell+BoxCollider2D), weaponTilePrefab (with WeaponTile).
//    - TetrominoSpawner: assign same GameSettingsSO, your TetrominoSO[] catalog, tetrominoPrefab (has TetrominoInstance+DragController).
//    - BattleController: assign GameSettingsSO + TetrominoSpawner.
//    - WaveManager: empty component for now.
// 3) Prefabs:
//    - gridCellPrefab: empty GameObject with GridCell + BoxCollider2D (size ~ cellSize, isTrigger).
//    - weaponTilePrefab: GameObject with SpriteRenderer + WeaponTile. Assign default WeaponSO or leave null if you want runtime randomization.
//    - tetrominoPrefab: empty root with TetrominoInstance + DragController + (optionally a visual child made of squares). DragController needs refs at runtime (assigned by Spawner).
// 4) Buttons:
//    - Hook UI Button to BattleController.StartBattle() and another to WaveManager.EndWave().
//
// That’s it – you have:
// - Configurable grid (via SO),
// - 3-piece rack spawning under grid,
// - Drag-and-drop onto grid, activating covered WeaponTiles that tick their Attack,
// - StartBattle clears rack + locks drag; EndWave re-spawns rack,
// - Merge when placing exactly over same-shape & level piece.
// Extend: integrate your zombie system by listening to WeaponTile.OnAttackTick to spawn bullets toward nearest zombie.
