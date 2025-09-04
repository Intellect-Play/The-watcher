using UnityEngine;

public class WaveManager : MonoBehaviour
{
    public void StartWave()
    {
        Debug.Log("Wave started.");
        // TODO: spawn zombies, run wave logic, and upon completion call EndWave()
    }

    public void EndWave()
    {
        Debug.Log("Wave ended.");
        FindObjectOfType<BattleController>()?.OnWaveEnded();
    }
}
