using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CardDatabase",
    menuName = "TCG/Card Database"
)]
public class CardDatabase : ScriptableObject
{
    [SerializeField]
    private List<CardData> allCards = new List<CardData>();

    public IReadOnlyList<CardData> AllCards => allCards;
}