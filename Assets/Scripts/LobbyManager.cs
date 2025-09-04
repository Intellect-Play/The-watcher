using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

[Serializable]
public class UICard
{
    public GameObject        lockIcon;
    public TextMeshProUGUI   lvlTxt;
    public GameObject        upgradeBtn;
}

public class LobbyManager : MonoBehaviour
{
    [Header("Boards")]
    public GameObject[] arenaBoards;
    public GameObject   rankingBoard;
    public GameObject   heroesBoard;
    // 0 → Inventor, 1 → Wizard, 2 → Samurai
    public GameObject[] heroSingleBoards;

    [Header("Bars")]
    public TextMeshProUGUI coinsTxt;
    public TextMeshProUGUI gemsTxt;

    [Header("Arena Progress")]
    public TextMeshProUGUI areaTxt;

    [Header("Bottom-Bar Buttons")]
    public Image rankingButtonImage;
    public Image heroesButtonImage;
    public Image battleButtonImage;

    [Header("UI cards for each Hero")]
    public UICard[] inventorCards;
    public UICard[] wizardCards;
    public UICard[] samuraiCards;

    // Hex colors: #1E2440 for normal, #4452D5 for focused
    readonly Color32 normalColor  = new Color32(0x1E, 0x24, 0x40, 255);
    readonly Color32 focusedColor = new Color32(0x44, 0x52, 0xD5, 255);

    // Keys for unlock/level state (order must match UI order)
    private readonly Dictionary<string,string[]> cardPrefKeys = new Dictionary<string,string[]>
    {
        { "Inventor", new[]{ "inv_bee","inv_bomb","inv_cog","inv_sword" } },
        { "Wizard",   new[]{ "wiz_dagger","wiz_feather","wiz_ring","wiz_stone" } },
        { "Samurai",  new[]{ "sam_arrow","sam_hammer","sam_knife","sam_shuriken" } },
    };

    void Start()
    {
        EnsureFirstTwoCardsDefaults(); // Ensure 0th and 1st cards are unlocked & level ≥ 1

        UpdateCurrencyBars();
        UpdateArenaProgressText();
        ShowBattle(); // default selection
    }

    /// <summary>
    /// Guarantees that the 0th and 1st cards of each deck are at least level 1 and unlocked.
    /// </summary>
    private void EnsureFirstTwoCardsDefaults()
    {
        foreach (var kv in cardPrefKeys)
        {
            var keys = kv.Value;
            if (keys == null || keys.Length == 0) continue;

            // Loop over first two cards (if they exist)
            for (int i = 0; i < Mathf.Min(2, keys.Length); i++)
            {
                string cardKey = keys[i];
                string levelKey = $"{cardKey}_level";

                int current = PlayerPrefs.GetInt(levelKey, 0);
                if (current < 1)
                    PlayerPrefs.SetInt(levelKey, 1); // at least level 1

                PlayerPrefs.SetInt(cardKey, 1); // mark as unlocked
            }
        }
        PlayerPrefs.Save();
    }

    private void UpdateCurrencyBars()
    {
        coinsTxt.text = PlayerPrefs.GetInt("gold", 0).ToString();
        gemsTxt.text  = PlayerPrefs.GetInt("gems", 0).ToString();
    }

    private void UpdateArenaProgressText()
    {
        if (PlayerPrefs.HasKey("arenaProgress"))
        {
            areaTxt.text = PlayerPrefs.GetString("arenaProgress");
        }
        else
        {
            int done  = PlayerPrefs.GetInt("battlesCompleted", 0);
            int total = PlayerPrefs.GetInt("battlesTotal", 10);
            areaTxt.text = $"{done}/{total}";
        }
    }

    private void ResetAllButtonBackgrounds()
    {
        rankingButtonImage.color = normalColor;
        heroesButtonImage.color  = normalColor;
        battleButtonImage.color  = normalColor;
    }

    private void FocusButton(Image btnImage)
    {
        ResetAllButtonBackgrounds();
        btnImage.color = focusedColor;
    }

    public void ShowRanking()
    {
        foreach (var board in arenaBoards) board.SetActive(false);
        rankingBoard.SetActive(true);
        heroesBoard.SetActive(false);
        HideAllHeroPages();
        FocusButton(rankingButtonImage);
    }

    public void ShowHeroes()
    {
        foreach (var board in arenaBoards) board.SetActive(false);
        rankingBoard.SetActive(false);
        heroesBoard.SetActive(true);
        HideAllHeroPages();
        FocusButton(heroesButtonImage);
    }

    public void ShowBattle()
    {
        rankingBoard.SetActive(false);
        heroesBoard.SetActive(false);
        for (int i = 0; i < arenaBoards.Length; i++)
            arenaBoards[i].SetActive(i == 0);
        HideAllHeroPages();
        FocusButton(battleButtonImage);
    }

    private void HideAllHeroPages()
    {
        foreach (var page in heroSingleBoards)
            page.SetActive(false);
    }

    /// <summary>
    /// Open the detailed page for the given hero deck.
    /// deckName should be "Inventor", "Wizard", or "Samurai".
    /// Uses only PlayerPrefs to populate lvlTxt, lock icon, and upgrade button.
    /// </summary>
    public void ShowHeroPage(string deckName)
    {
        // Hide other UI
        foreach (var board in arenaBoards) board.SetActive(false);
        rankingBoard.SetActive(false);
        heroesBoard.SetActive(false);
        HideAllHeroPages();

        // Show selected hero page
        int pageIndex = deckName == "Inventor" ? 0
                       : deckName == "Wizard"   ? 1
                                                : 2;
        heroSingleBoards[pageIndex].SetActive(true);

        // Select UI array
        UICard[] uiCards = deckName == "Inventor" ? inventorCards
                         : deckName == "Wizard"   ? wizardCards
                                                  : samuraiCards;

        // Get keys for this deck
        if (!cardPrefKeys.TryGetValue(deckName, out var keys) || uiCards == null) return;

        // Populate UI from PlayerPrefs
        int count = Mathf.Min(uiCards.Length, keys.Length);
        for (int i = 0; i < count; i++)
        {
            string key = keys[i];
            string levelKey = $"{key}_level";

            bool hasLevelKey = PlayerPrefs.HasKey(levelKey);
            int  level       = hasLevelKey ? PlayerPrefs.GetInt(levelKey, 0) : 0;

            // Level text visibility and value
            uiCards[i].lvlTxt.gameObject.SetActive(hasLevelKey && level >= 1);
            if (hasLevelKey && level >= 1)
                uiCards[i].lvlTxt.text = level.ToString();

            // Lock icon
            bool isLocked = !hasLevelKey || level < 1;
            uiCards[i].lockIcon.SetActive(isLocked);

            // Upgrade button
            uiCards[i].upgradeBtn.SetActive(hasLevelKey && level >= 1);
        }

        // Highlight Heroes button
        FocusButton(heroesButtonImage);
    }
}
