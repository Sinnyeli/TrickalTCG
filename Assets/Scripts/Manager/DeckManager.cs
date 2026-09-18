using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [SerializeField] private DeckData deckData;

    private List<RuntimeCard> drawPile =
        new List<RuntimeCard>();

    private List<RuntimeCard> discardPile =
        new List<RuntimeCard>();

    public RuntimeCard Commander { get; private set; }
    [SerializeField]
    private PlayerSide owner;

    public PlayerSide Owner => owner;
    public int RemainingCards => drawPile.Count;

  public void InitializeDeck()
{
    drawPile.Clear();
    discardPile.Clear();

    // =========================================
    // CREATE COMMANDER
    // =========================================

    if (deckData.commander != null)
    {
        Commander = new RuntimeCard(
            deckData.commander,
            true
        );

        Commander.SetOwner(owner);

        Commander.ChangeZone(
            CardZone.Hand
        );
    }
    else
    {
        Debug.LogError(
            $"{owner} deck has no Commander assigned!"
        );
    }

    // =========================================
    // CREATE NORMAL DECK
    // =========================================

    foreach (CardData cardData in deckData.cards)
    {
        if (cardData == null)
            continue;

        RuntimeCard card =
            new RuntimeCard(cardData);

        card.SetOwner(owner);

        card.ChangeZone(
            CardZone.Deck
        );

        drawPile.Add(card);
    }

    // =========================================
    // SHUFFLE
    // =========================================

    Shuffle();

    Debug.Log(
        $"{owner} deck initialized. " +
        $"Commander: {Commander?.Data.cardName}, " +
        $"Cards: {drawPile.Count}"
    );
}
    public int GetDeckCount()
    {
        return drawPile.Count;
    }

    public RuntimeCard GetCommander()
    {
        return Commander;
    }

    // =========================================
    // DRAW
    // =========================================

    public RuntimeCard DrawCard()
    {
        if (drawPile.Count == 0)
        {
            Debug.Log("Deck is empty.");
            return null;
        }

        RuntimeCard card =
            drawPile[0];

        drawPile.RemoveAt(0);

        card.ChangeZone(
            CardZone.Hand
        );

        return card;
    }

    // =========================================
    // ADD CARD TO DECK
    // =========================================

    public void AddCardToDeck(
        CardData cardData,
        bool shuffle = true)
    {
        if (cardData == null)
            return;

        RuntimeCard card =
            new RuntimeCard(cardData);

        card.ChangeZone(
            CardZone.Deck
        );

        drawPile.Add(card);

        if (shuffle)
        {
            Shuffle();
        }

        Debug.Log(
            $"{cardData.cardName} added to deck. " +
            $"Deck now contains {drawPile.Count} cards."
        );
    }

    // =========================================
    // DISCARD
    // =========================================

    public void Discard(RuntimeCard card)
    {
        if (card == null)
            return;

        card.ChangeZone(
            CardZone.Graveyard
        );

        discardPile.Add(card);
    }

    // =========================================
    // SHUFFLE
    // =========================================

    public void Shuffle()
    {
        for (int i = 0;
             i < drawPile.Count;
             i++)
        {
            int randomIndex =
                Random.Range(
                    i,
                    drawPile.Count
                );

            RuntimeCard temp =
                drawPile[i];

            drawPile[i] =
                drawPile[randomIndex];

            drawPile[randomIndex] =
                temp;
        }
    }
}