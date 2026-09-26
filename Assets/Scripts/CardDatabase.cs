using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CardDatabase",
    menuName = "Cards/Card Database"
)]
public class CardDatabase : ScriptableObject
{
    [SerializeField]
    private List<CardData> allCards =
        new List<CardData>();

    public IReadOnlyList<CardData> AllCards =>
        allCards;


    // =========================================================
    // FIND CARD BY ID
    // =========================================================

    public CardData GetCardByID(
        string cardID)
    {
        if (string.IsNullOrWhiteSpace(cardID))
        {
            Debug.LogWarning(
                "GetCardByID called with an empty Card ID."
            );

            return null;
        }


        foreach (CardData card in allCards)
        {
            if (card == null)
                continue;


            if (card.CardID == cardID)
            {
                return card;
            }
        }


        Debug.LogWarning(
            $"CardDatabase could not find Card ID: {cardID}"
        );

        return null;
    }
}