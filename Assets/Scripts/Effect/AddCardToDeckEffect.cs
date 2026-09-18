using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "AddCardToDeckEffect",
    menuName = "Card Effects/Add Card To Deck"
)]
public class AddCardToDeckEffect : CardEffect
{
    [Header("Card To Add")]
    [SerializeField]
    private CardData cardToAdd;

    [Header("Amount")]
    [SerializeField]
    private int amount = 1;

    [Header("Target Deck")]
    [SerializeField]
    private DeckTargetType targetDeck =
        DeckTargetType.Friendly;

    [Header("Shuffle")]
    [SerializeField]
    private bool shuffle = true;

    public override void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets)
    {
        if (source == null)
            return;

        if (cardToAdd == null)
            return;

        switch (targetDeck)
        {
            case DeckTargetType.Friendly:
                AddToDeck(
                    source,
                    source.Owner
                );
                break;

            case DeckTargetType.Enemy:
                AddToDeck(
                    source,
                    GetOpponent(source.Owner)
                );
                break;

            case DeckTargetType.Both:
                AddToDeck(
                    source,
                    source.Owner
                );

                AddToDeck(
                    source,
                    GetOpponent(source.Owner)
                );
                break;
        }
    }

    private void AddToDeck(
        RuntimeCard source,
        PlayerSide side)
    {
        DeckManager deck =
            GameManager.Instance.GetDeck(side);

        if (deck == null)
            return;

        for (int i = 0; i < amount; i++)
        {
            deck.AddCardToDeck(
                cardToAdd,
                false
            );
        }

        if (shuffle)
        {
            deck.Shuffle();
        }

        Debug.Log(
            $"{source.Data.cardName} added " +
            $"{amount} copies of {cardToAdd.cardName} " +
            $"to {side}'s deck."
        );
    }

    private PlayerSide GetOpponent(
        PlayerSide side)
    {
        return side == PlayerSide.Player
            ? PlayerSide.Opponent
            : PlayerSide.Player;
    }
}