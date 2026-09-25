using UnityEngine;
using System.Collections.Generic;

public abstract class CardData : ScriptableObject
{
    [Header("Basic Information")]
    [SerializeField]
    private string cardID;
    public string CardID => cardID;
    public string cardName;

    [TextArea]
    public string description;

    [Header("Cost")]
    public int manaCost;

    [Header("Visuals")]
    public Sprite artwork;

  [Header("On Draw")]
    [SerializeField]
    private List<CardEffect> onDrawEffects =
        new List<CardEffect>();

    public IReadOnlyList<CardEffect> OnDrawEffects =>
        onDrawEffects;

    [Header("Deck Building")]
    [SerializeField]
    private bool collectible = true;
    

    public bool Collectible => collectible;
    [SerializeField]
    private bool unlimitedCopies = false;

    public bool UnlimitedCopies => unlimitedCopies;

    
}