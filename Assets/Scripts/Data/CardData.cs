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
    public string cardName;
    [TextArea]
    public string description;

    [Header("Visuals")]
    public Sprite artwork;

    [Header("Cost")]
    public int manaCost;
}