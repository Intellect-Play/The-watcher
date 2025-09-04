// RoguelikeManager.cs
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RoguelikeManager : MonoBehaviour
{
    [Header("UI & Deck References")]
    public UIManager          uiManager;
    public CardDeckAnimator[] cardDeckAnimators;

    [Header("Hero Definitions")]
    public HeroSO inventorSO;
    public HeroSO wizardSO;
    public HeroSO samuraiSO;

    [Header("Hero Deck Cards (must match key order)")]
    public CardSO[] inventorCards;
    public CardSO[] wizardCards;
    public CardSO[] samuraiCards;

    // runtime state
    public string ActiveHeroDeckName { get; private set; }
    public List<RoguelikeOption> CurrentOptions { get; private set; }

    // PlayerPrefs keys for card unlocks
    private readonly Dictionary<string,string[]> cardPrefKeys = new Dictionary<string,string[]>
    {
        { "Inventor", new[]{ "inv_bee","inv_bomb","inv_cog","inv_sword" } },
        { "Wizard",   new[]{ "wiz_dagger","wiz_feather","wiz_ring","wiz_stone" } },
        { "Samurai",  new[]{ "sam_arrow","sam_hammer","sam_knife","sam_shuriken" } },
    };

    public void InitializeHeroSelection()
    {
        PlayerPrefs.SetInt("Deck_Inventor_Active", 0);
        PlayerPrefs.SetInt("Deck_Wizard_Active",   0);
        PlayerPrefs.SetInt("Deck_Samurai_Active",  0);
        PlayerPrefs.SetInt("Hero_Inventor", 0);
        PlayerPrefs.SetInt("Hero_Wizard",   0);
        PlayerPrefs.SetInt("Hero_Samurai",  0);
        foreach (var kv in cardPrefKeys)
            foreach (var cardKey in kv.Value)
                PlayerPrefs.SetInt(cardKey, 1);
        PlayerPrefs.Save();
    }

    public void SetActiveHero(string deckName)
    {
        ActiveHeroDeckName = deckName;
        PlayerPrefs.SetInt($"Hero_{deckName}",       1);
        PlayerPrefs.SetInt($"Deck_{deckName}_Active",1);
        PlayerPrefs.Save();
    }

    public List<RoguelikeOption> GetUnlockedHeroOptions()
    {
        var list = new List<RoguelikeOption>();
        if (PlayerPrefs.GetInt("Hero_Inventor", 0) == 1)
            list.Add(new RoguelikeOption { type = OptionType.NewHero, deckName = "Inventor", targetHero = inventorSO });
        if (PlayerPrefs.GetInt("Hero_Wizard",   0) == 1)
            list.Add(new RoguelikeOption { type = OptionType.NewHero, deckName = "Wizard",   targetHero = wizardSO   });
        if (PlayerPrefs.GetInt("Hero_Samurai",  0) == 1)
            list.Add(new RoguelikeOption { type = OptionType.NewHero, deckName = "Samurai",  targetHero = samuraiSO  });
        if (list.Count == 0)
        {
            list.Add(new RoguelikeOption { type = OptionType.NewHero, deckName = "Inventor", targetHero = inventorSO });
            list.Add(new RoguelikeOption { type = OptionType.NewHero, deckName = "Wizard",   targetHero = wizardSO   });
            list.Add(new RoguelikeOption { type = OptionType.NewHero, deckName = "Samurai",  targetHero = samuraiSO  });
        }
        return list;
    }

    private CardSO[] GetCardsForDeck(string deckName)
    {
        return deckName switch
        {
            "Inventor" => inventorCards,
            "Wizard"   => wizardCards,
            "Samurai"  => samuraiCards,
            _          => null
        };
    }

    private List<CardSO> GetUnlockedCards(string deckName)
    {
        var result = new List<CardSO>();
        var cards = GetCardsForDeck(deckName);
        if (cards == null || !cardPrefKeys.ContainsKey(deckName))
            return result;

        var keys = cardPrefKeys[deckName];
        int count = Mathf.Min(cards.Length, keys.Length);
        for (int i = 0; i < count; i++)
            if (PlayerPrefs.GetInt(keys[i], 0) > 0)
                result.Add(cards[i]);

        return result;
    }

    public IEnumerator RunRoguelike(int waveNumber, List<RoguelikeOption> options)
    {
        var activeDecks = new List<string>();
        if (PlayerPrefs.GetInt("Deck_Inventor_Active", 0) == 1) activeDecks.Add("Inventor");
        if (PlayerPrefs.GetInt("Deck_Wizard_Active",   0) == 1) activeDecks.Add("Wizard");
        if (PlayerPrefs.GetInt("Deck_Samurai_Active",  0) == 1) activeDecks.Add("Samurai");
        if (activeDecks.Count == 0) yield break;

        var usedCards = new HashSet<CardSO>();
        for (int i = 0; i < options.Count; i++)
        {
            var opt = options[i];
            if (opt.type == OptionType.NewHero)
            {
                var candidates = new List<string> { "Inventor", "Wizard", "Samurai" }
                    .Where(h => PlayerPrefs.GetInt($"Deck_{h}_Active", 0) == 0).ToList();
                if (candidates.Count == 0)
                    candidates = new List<string> { "Inventor", "Wizard", "Samurai" };

                string deck = candidates[Random.Range(0, candidates.Count)];
                opt.deckName       = deck;
                opt.targetDeckName = deck;
                opt.targetHero     = deck == "Inventor" ? inventorSO
                                  : deck == "Wizard"   ? wizardSO
                                                       : samuraiSO;
            }
            else if (opt.type == OptionType.UpgradeCard)
            {
                string deck = activeDecks[Random.Range(0, activeDecks.Count)];
                opt.deckName       = deck;
                opt.targetDeckName = deck;

                var unlocked   = GetUnlockedCards(deck);
                var candidates = unlocked.Where(c => !usedCards.Contains(c)).ToList();
                if (candidates.Count == 0) candidates = unlocked;
                if (candidates.Count == 0) continue;

                var pick = candidates[Random.Range(0, candidates.Count)];
                opt.targetCard = pick;
                opt.oldLevel   = pick.sessionLevel;
                opt.newLevel   = pick.sessionLevel + 1;
                usedCards.Add(pick);
            }
            else if (opt.type == OptionType.ReduceCooldown)
            {
                string deck = activeDecks[Random.Range(0, activeDecks.Count)];
                opt.deckName       = deck;
                opt.targetDeckName = deck;

                var deckCards = GetCardsForDeck(deck);
                if (deckCards != null && deckCards.Length > 0)
                    opt.targetCard = deckCards[^1];
            }
        }

        CurrentOptions = options;
        uiManager.SetRougelikeText($"Wave {waveNumber} complete!\nChoose an option:");
        uiManager.ShowRoguelikeOptions();
        uiManager.DisableSlotsWithoutImage();
        uiManager.SetRougelikeBoardActive(true);

        yield return new WaitUntil(() => !uiManager.roguelikeBoard.activeSelf);

        int choice = uiManager.selectedRoguelikeIndex;
        if (choice < 0 || choice >= options.Count) yield break;

        var picked = options[choice];
        int idx = picked.deckName == "Inventor" ? 0
                : picked.deckName == "Wizard"   ? 1
                                                : 2;
        var anim = cardDeckAnimators[idx];
        anim.gameObject.SetActive(true);

        if (picked.type == OptionType.NewHero)
        {
            SetActiveHero(picked.deckName);
        }
        else if (picked.type == OptionType.UpgradeCard)
        {
            // increment sessionLevel and update preview values
            int prevLevel = picked.targetCard.sessionLevel;
            picked.targetCard.IncrementSessionLevel();
            picked.oldLevel = prevLevel;
            picked.newLevel = picked.targetCard.sessionLevel;
        }
        else // ReduceCooldown
        {
            anim.ReduceShuffleDelay(20);
        }
    }
}
