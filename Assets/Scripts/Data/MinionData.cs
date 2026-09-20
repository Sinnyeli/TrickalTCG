using System.Collections.Generic;
using UnityEngine;

public abstract class MinionData : CardData
{
    [Header("Stats")]
    public int attack;
    public int health;

    [Header("Race")]
    public CardRace cardRace;

    [Header("Keywords")]
    [SerializeField]
    private List<CardKeyword> keywords =
        new List<CardKeyword>();

    public IReadOnlyList<CardKeyword> Keywords =>
        keywords;

    public bool HasKeyword(
        CardKeyword keyword)
    {
        return keywords != null &&
               keywords.Contains(keyword);
    }

    // =========================================
    // MINION TRIGGERS
    // =========================================

    [Header("Minion Effects")]
    [SerializeField]
    private CardEffect battlecry;

    [SerializeField]
    private CardEffect deathrattle;

    [SerializeField]
    private CardEffect passive;

    [SerializeField]
    private CardEffect resonance;

    [SerializeField]
    private CardEffect turnStart;

    [SerializeField]
    private CardEffect turnEnd;

    [SerializeField]
    private CardEffect onDamageTaken;

    [SerializeField]
    private CardEffect onAttack;

    public CardEffect Battlecry => battlecry;
    public CardEffect Deathrattle => deathrattle;
    public CardEffect Passive => passive;
    public CardEffect Resonance => resonance;
    public CardEffect TurnStart => turnStart;
    public CardEffect TurnEnd => turnEnd;
    public CardEffect OnDamageTaken => onDamageTaken;
    public CardEffect OnAttack => onAttack;
}