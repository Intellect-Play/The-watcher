using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EnemyCount
{
    [Tooltip("Reference to the Enemy ScriptableObject")]
    public EnemySO enemy;
    [Tooltip("Number of enemies to spawn")]
    public int count;
}

public enum OptionType
{
    UpgradeCard,
    ReduceCooldown,
    NewHero
}

[Serializable]
public class RoguelikeOption
{
    public OptionType type;
    public string     deckName;

    // for UpgradeCard & ReduceCooldown
    [HideInInspector] public CardSO targetCard;
    [HideInInspector] public string targetDeckName;

    // for NewHero
    [HideInInspector] public HeroSO targetHero;

    // preview levels for UpgradeCard
    public int oldLevel;
    public int newLevel;

    internal Sprite overrideArtwork;

    public Sprite GetArtwork()
    {
        if (overrideArtwork != null)
            return overrideArtwork;

        switch (type)
        {
            case OptionType.NewHero:
                return targetHero?.cardImg;
            default:
                return targetCard?.artwork;
        }
    }
}


[Serializable]
public class Wave
{
    [Header("Gold Amount Multiplier")]
    public float goldAmountMultiplier = 1.0f;

    [Header("Wave Composition")]
    public List<EnemyCount> enemies = new List<EnemyCount>();

    [Header("Roguelike Options")]
    public List<RoguelikeOption> roguelikeOptions = new List<RoguelikeOption>();
}
