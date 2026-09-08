using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum CardType
{
    Monster,
    Apostle,
    Spell,
    Artifact
}
public abstract class CardData : ScriptableObject
{
    [Header("Basic Information")]
    [SerializeField]private string cardID;
    public string CardID => cardID;
    public string cardName;
    [TextArea]
    public string description;

    [Header("Effects")]
    [SerializeField] private CardEffect battlecry;
    [SerializeField] private CardEffect deathrattle;
    [SerializeField] private CardEffect passive;

    public CardEffect Battlecry => battlecry;
    public CardEffect Deathrattle => deathrattle;
    public CardEffect Passive => passive;


    [Header("Keywords")]
    [SerializeField] private List<CardKeyword> keywords;

    public bool HasKeyword(CardKeyword keyword)
    {
        return keywords != null &&
               keywords.Contains(keyword);
    }


    [Header("Visuals")]
    public Sprite artwork;

    [Header("Cost")]
    public int manaCost;
}