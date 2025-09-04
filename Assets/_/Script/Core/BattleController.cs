using UnityEngine;

public class BattleController : MonoBehaviour
{
    public GameSettingsSO settings;
    public TetrominoSpawner spawner;

    public bool IsBattleRunning { get; private set; }

    void Start()
    {
        if (settings.dragEnabledAtStart)
            spawner.SpawnRack();
    }

    public void StartBattle()
    {
        if (IsBattleRunning) return;
        IsBattleRunning = true;

        // Remove leftover rack pieces and disable dragging while in battle
        spawner.SetRackDraggable(false);
        spawner.ClearRack();

        // TODO: trigger your wave/zombie system start here
        FindObjectOfType<WaveManager>()?.StartWave();
    }

    public void OnWaveEnded()
    {
        if (!IsBattleRunning) return;
        IsBattleRunning = false;

        // Spawn new rack and re-enable dragging
        spawner.SpawnRack();
    }
}
