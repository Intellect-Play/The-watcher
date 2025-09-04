using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public LevelManager        levelManager;
    public UIManager           uIManager;
    public RoguelikeManager    roguelikeManager;
    public CardDeckAnimator[]  cardDeckAnimators;   // 0=Inventor, 1=Wizard, 2=Samurai
    public Transform           enemyParent;

    [Header("Spawn Settings")]
    public float enemySpawnY    = 3.5f;
    public float enemySpawnXMin = -1.5f;
    public float enemySpawnXMax = 1.5f;
    public float checkInterval  = 0.1f;

    [Header("Fortress Health")]
    public int health = 100;

    [Header("Wave Flow")]
    [Tooltip("World-space Y threshold; when a wave’s front-most enemy crosses below this, spawn the next wave.")]
    public float midpointY = 0f;

    // --- Runtime state ---
    private Camera mainCam;

    private const string PrefNextLevelIndex = "NextLevelIndex";
    private int _currentLevelIndex = 0;

    // Pause helpers
    private int  _pauseDepth  = 0;
    private bool _loseStarted = false;

    // Per-wave bookkeeping
    private Transform[] _waveRoots;
    private int[]       _waveCountsDeclared;
    private int[]       _cumuCounts;
    private bool[]      _waveSpawnFinished;
    private bool[]      _waveSpawned;
    private bool[]      _roguelikeShown;

    // Kills (global)
    private int _totalEnemies;
    private int _totalKills;

    // Global subscription guard
    private bool _subscribed = false;

    // === PlayerPrefs keys we reset between levels ===
    private static readonly string[] DeckActiveKeys = {
        "Deck_Inventor_Active", "Deck_Wizard_Active", "Deck_Samurai_Active"
    };
    private static readonly string[] HeroKeys = {
        "Hero_Inventor", "Hero_Wizard", "Hero_Samurai"
    };
    private static readonly string[] CardUnlockKeys = {
        "inv_bee","inv_bomb","inv_cog","inv_sword",
        "wiz_dagger","wiz_feather","wiz_ring","wiz_stone",
        "sam_arrow","sam_hammer","sam_knife","sam_shuriken"
    };

    // =================== Unity ===================

    void Awake()
    {
        _currentLevelIndex = Mathf.Clamp(PlayerPrefs.GetInt(PrefNextLevelIndex, 0), 0, levelManager.levels.Length - 1);
    }

    void Start()
    {
        mainCam = Camera.main;

        uIManager.SetCoins(PlayerPrefs.GetInt("gold", 0));
        uIManager.SetHealthText(health.ToString());
        uIManager.SetWinBoardActive(false);
        uIManager.SetLoseBoardActive(false);

        // IMPORTANT: start with ALL decks invisible/inactive
        DeactivateAllDecks();

        StartCoroutine(InitialHeroSelectionAndThenStart());
    }

    void Update()
    {
        if (health <= 0 && !_loseStarted)
        {
            _loseStarted = true;
            health = 0;
            uIManager.SetHealthText("0");
            StartCoroutine(LoseSequence());
        }
    }

    void OnDestroy()
    {
        if (_subscribed)
        {
            EnemyBehaviour.OnEnemyDestroyed -= HandleEnemyDestroyed;
            _subscribed = false;
        }
    }

    // =================== Deck helpers ===================

    private void DeactivateAllDecks()
    {
        if (cardDeckAnimators == null) return;

        foreach (var cda in cardDeckAnimators)
        {
            if (!cda) continue;
            cda.PauseFiring();
            var parent = cda.transform.parent ? cda.transform.parent.gameObject : null;
            if (cda.gameObject.activeSelf) cda.gameObject.SetActive(false);
            if (parent && parent.activeSelf) parent.SetActive(false);
        }
    }

    private void ActivateDecksFromPrefs()
    {
        if (cardDeckAnimators == null) return;

        string[] keys = DeckActiveKeys;

        for (int i = 0; i < cardDeckAnimators.Length && i < keys.Length; i++)
        {
            var cda = cardDeckAnimators[i];
            if (!cda) continue;

            bool wantActive = PlayerPrefs.GetInt(keys[i], 0) == 1;
            var parent = cda.transform.parent ? cda.transform.parent.gameObject : null;

            if (wantActive)
            {
                if (parent && !parent.activeSelf) parent.SetActive(true);
                if (!cda.gameObject.activeSelf)  cda.gameObject.SetActive(true);
                cda.ResumeFiring();
            }
            else
            {
                cda.PauseFiring();
                if (cda.gameObject.activeSelf) cda.gameObject.SetActive(false);
                if (parent && parent.activeSelf) parent.SetActive(false);
            }
        }
    }

    // Legacy wrapper for UIManager
    public void ActivateDeckAnimatorParent(int index)
    {
        if (cardDeckAnimators == null || index < 0 || index >= cardDeckAnimators.Length) return;
        var animator = cardDeckAnimators[index];
        if (!animator) return;

        var parent = animator.transform.parent ? animator.transform.parent.gameObject : null;
        if (parent && !parent.activeSelf) parent.SetActive(true);
        if (!animator.gameObject.activeSelf) animator.gameObject.SetActive(true);

        animator.ResumeFiring();
    }

    // =================== Pause helpers ===================

    private void PauseGameForRoguelike()
    {
        _pauseDepth++;
        if (_pauseDepth == 1)
        {
            if (cardDeckAnimators != null)
                foreach (var cda in cardDeckAnimators)
                    if (cda) cda.PauseFiring();

            Time.timeScale = 0f;
            AudioListener.pause = true;
        }
    }

    private void ResumeGameAfterRoguelike()
    {
        _pauseDepth = Mathf.Max(0, _pauseDepth - 1);
        if (_pauseDepth == 0)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (cardDeckAnimators != null)
                foreach (var cda in cardDeckAnimators)
                    if (cda && cda.gameObject.activeInHierarchy) cda.ResumeFiring();
        }
    }

    // =================== Intro / Lose ===================

    private IEnumerator InitialHeroSelectionAndThenStart()
    {
        // Start with all decks invisible — NO activation here

        // Hero pick UI primes prefs (RoguelikeManager sets deck flags)
        roguelikeManager.InitializeHeroSelection();
        uIManager.SetRougelikeText("Pick a hero");

        var heroOptions = roguelikeManager.GetUnlockedHeroOptions();
        for (int i = 0; i < heroOptions.Count && i < uIManager.offeredCardSlots.Length; i++)
            uIManager.SetRoguelikeOption(i, heroOptions[i]);
        uIManager.DisableSlotsWithoutImage();

        uIManager.SetRougelikeBoardActive(true);
        PauseGameForRoguelike();

        yield return new WaitUntil(() => !uIManager.roguelikeBoard.activeSelf);

        ResumeGameAfterRoguelike();

        // NOW turn on only the selected/active decks
        ActivateDecksFromPrefs();

        StartCoroutine(RunLevel());
    }

    private IEnumerator LoseSequence()
    {
        yield return new WaitForSecondsRealtime(2f);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        uIManager.SetLoseBoardActive(true);
        enabled = false;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health < 0) health = 0;
        uIManager.SetHealthText(health.ToString());
    }

    // =================== Main Level Flow ===================

    private IEnumerator RunLevel()
    {
        var level      = levelManager.levels[_currentLevelIndex];
        var waves      = level.waves;
        int totalWaves = waves.Length;

        // Allocate arrays
        _waveRoots          = new Transform[totalWaves];
        _waveCountsDeclared = new int[totalWaves];
        _cumuCounts         = new int[totalWaves];
        _waveSpawnFinished  = new bool[totalWaves];
        _waveSpawned        = new bool[totalWaves];
        _roguelikeShown     = new bool[totalWaves];

        _totalEnemies = 0;
        _totalKills   = 0;

        // Count & parents + cumulative thresholds
        for (int i = 0; i < totalWaves; i++)
        {
            int c = 0;
            foreach (var ec in waves[i].enemies) c += ec.count;
            _waveCountsDeclared[i] = c;
            _totalEnemies += c;

            _cumuCounts[i] = (i == 0) ? c : _cumuCounts[i - 1] + c;

            var root = new GameObject($"Wave {i + 1}");
            root.transform.SetParent(enemyParent, false);
            _waveRoots[i] = root.transform;
        }

        // UI init
        uIManager.SetLevelText($"Lvl {_currentLevelIndex + 1}");
        uIManager.SetWaveText($"Wave {Mathf.Min(1, totalWaves)}/{totalWaves}");
        uIManager.SetSliderValue(0f);

        // Subscribe once
        if (!_subscribed)
        {
            EnemyBehaviour.OnEnemyDestroyed += HandleEnemyDestroyed;
            _subscribed = true;
        }

        // Spawn first wave
        yield return SpawnWave(0, _waveCountsDeclared[0], waves[0], _waveRoots[0]);
        _waveSpawnFinished[0] = true;
        _waveSpawned[0]       = true;

        // Coordinators
        StartCoroutine(MidpointSpawnCoordinator(waves));
        StartCoroutine(RoguelikeCoordinator(waves));

        // Win when all declared enemies die
        yield return new WaitUntil(() => _totalKills >= _totalEnemies);

        yield return new WaitForSecondsRealtime(2f);
        Time.timeScale = 0f;
        AudioListener.pause = true;
        uIManager.SetWinBoardActive(true);
    }

    private IEnumerator MidpointSpawnCoordinator(Wave[] waves)
    {
        int totalWaves = waves.Length;

        for (int wi = 0; wi < totalWaves - 1; wi++)
        {
            yield return new WaitUntil(() => _waveSpawnFinished[wi]);
            yield return new WaitUntil(() => FrontMostBelowMidpoint(wi) || WaveIsFullyDeadByCumulative(wi));

            int next = wi + 1;
            if (!_waveSpawned[next])
            {
                _waveSpawned[next] = true;
                yield return SpawnWave(next, _waveCountsDeclared[next], waves[next], _waveRoots[next]);
                _waveSpawnFinished[next] = true;
            }
        }
    }

    private bool WaveIsFullyDeadByCumulative(int waveIndex)
    {
        return _totalKills >= _cumuCounts[waveIndex];
    }

    private bool FrontMostBelowMidpoint(int waveIndex)
    {
        var root = _waveRoots[waveIndex];
        if (root == null) return true;

        bool anyAlive = false;
        float minY = float.MaxValue;

        foreach (Transform child in root)
        {
            if (!child || !child.gameObject.activeInHierarchy) continue;
            var eb = child.GetComponent<EnemyBehaviour>();
            if (!eb) continue;

            anyAlive = true;
            if (child.position.y < minY) minY = child.position.y;
        }

        if (!anyAlive) return true;

        return (minY <= midpointY);
    }

    // =================== Enemy death / UI progress ===================

    private void HandleEnemyDestroyed(EnemyBehaviour eb)
    {
        if (eb == null) return;

        _totalKills++;
        AwardGold(eb.rewardGold);
        UpdateWaveUIFromTotals();
    }

    private void UpdateWaveUIFromTotals()
    {
        int totalWaves = _waveCountsDeclared.Length;

        int completedWaves = 0;
        while (completedWaves < totalWaves && _totalKills >= _cumuCounts[completedWaves])
            completedWaves++;

        int labelWave = Mathf.Clamp(completedWaves + 1, 1, totalWaves);
        uIManager.SetWaveText($"Wave {labelWave}/{totalWaves}");

        if (completedWaves >= totalWaves)
        {
            uIManager.SetSliderValue(1f);
            return;
        }

        int prevThreshold   = (completedWaves == 0) ? 0 : _cumuCounts[completedWaves - 1];
        int killsIntoWave   = Mathf.Max(0, _totalKills - prevThreshold);
        int neededThisWave  = Mathf.Max(1, _waveCountsDeclared[completedWaves]);
        float progress      = Mathf.Clamp01(killsIntoWave / (float)neededThisWave);

        uIManager.SetSliderValue(progress);
    }

    // =================== Roguelikes after each wave ===================

    private IEnumerator RoguelikeCoordinator(Wave[] waves)
    {
        int totalWaves = waves.Length;

        for (int wi = 0; wi < totalWaves - 1; wi++) // none after last
        {
            yield return new WaitUntil(() => _totalKills >= _cumuCounts[wi]);

            if (_roguelikeShown[wi]) continue;
            _roguelikeShown[wi] = true;

            PauseGameForRoguelike();
            yield return roguelikeManager.RunRoguelike(wi + 1, waves[wi].roguelikeOptions);
            if (uIManager.roguelikeBoard.activeSelf)
                yield return new WaitUntil(() => !uIManager.roguelikeBoard.activeSelf);
            ResumeGameAfterRoguelike();

            // If a new deck was enabled, turn it on safely now
            ActivateDecksFromPrefs();
        }
    }

    // =================== Spawning ===================

    private IEnumerator SpawnWave(int wi, int enemiesThis, Wave wave, Transform waveRoot)
    {
        var queue = new List<EnemySO>(enemiesThis);
        bool added = true; int li = 0;
        while (queue.Count < enemiesThis && added)
        {
            added = false;
            foreach (var ec in wave.enemies)
                if (li < ec.count) { queue.Add(ec.enemy); added = true; }
            li++;
        }

        var xs = GenerateShuffledXPositions(enemySpawnXMin, enemySpawnXMax, enemiesThis);

        int sortOrder = enemiesThis;
        for (int i = 0; i < queue.Count; i++)
        {
            Vector3 pos = new Vector3(xs[i], enemySpawnY, 0f);
            while (!IsSpawnPositionClear(pos.x))
                yield return new WaitForSeconds(checkInterval);

            var go = Instantiate(queue[i].prefab, pos, Quaternion.identity, waveRoot);
            var eb = go.GetComponent<EnemyBehaviour>();
            eb.health           = queue[i].health;
            eb.attack           = queue[i].attack;
            eb.moveSpeed        = queue[i].moveSpeed;
            eb.rewardGold       = queue[i].rewardedGold;
            eb.onFortressDamage = TakeDamage;

            var sr = go.GetComponent<SpriteRenderer>();
            if (sr) sr.sortingOrder = sortOrder--;
        }
    }

    // =================== Util ===================

    private void AwardGold(int amount)
    {
        int g = PlayerPrefs.GetInt("gold", 0) + amount;
        PlayerPrefs.SetInt("gold", g);
        PlayerPrefs.Save();
        uIManager.SetCoins(g);
    }

    private List<float> GenerateShuffledXPositions(float min, float max, int count)
    {
        var pos = new List<float>();
        if (count <= 0) return pos;
        if (count == 1) { pos.Add((min + max) / 2f); return pos; }

        float step = (max - min) / (count - 1);
        for (int i = 0; i < count; i++) pos.Add(min + i * step);

        for (int i = 0; i < pos.Count; i++)
        {
            int j = Random.Range(i, pos.Count);
            float tmp = pos[i]; pos[i] = pos[j]; pos[j] = tmp;
        }
        return pos;
    }

    private bool IsSpawnPositionClear(float x)
    {
        foreach (Transform w in enemyParent)
            foreach (Transform e in w)
                if (Mathf.Abs(e.position.x - x) < 0.5f &&
                    Mathf.Abs(e.position.y - enemySpawnY) < 0.5f)
                    return false;
        return true;
    }

    // =================== Public: Next Level (HARD RESET) =====================

    public void NextLevel()
    {
        if (_subscribed)
        {
            EnemyBehaviour.OnEnemyDestroyed -= HandleEnemyDestroyed;
            _subscribed = false;
        }

        int next = _currentLevelIndex + 1;
        if (next >= levelManager.levels.Length) next = 0;

        PlayerPrefs.SetInt(PrefNextLevelIndex, next);

        ResetRunPrefs();
        ResetCardSessionLevelsToOne();
        PlayerPrefs.Save();

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ResetRunPrefs()
    {
        foreach (var k in DeckActiveKeys) PlayerPrefs.SetInt(k, 0);
        foreach (var k in HeroKeys)       PlayerPrefs.SetInt(k, 0);
        foreach (var k in CardUnlockKeys) PlayerPrefs.SetInt(k, 1);
        // Keep gold unless you want to reset it here.
    }

    private void ResetCardSessionLevelsToOne()
    {
        if (roguelikeManager != null)
        {
            void ResetArray(CardSO[] arr)
            {
                if (arr == null) return;
                foreach (var c in arr) { if (c) c.sessionLevel = 1; }
            }
            ResetArray(roguelikeManager.inventorCards);
            ResetArray(roguelikeManager.wizardCards);
            ResetArray(roguelikeManager.samuraiCards);
        }
    }
}
