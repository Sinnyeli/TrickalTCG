using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [SerializeField] private DeckData deckData;

    private List<RuntimeCard> drawPile = new List<RuntimeCard>();
    private List<RuntimeCard> discardPile = new List<RuntimeCard>();

    public RuntimeCard Commander { get; private set; }

    public int RemainingCards => drawPile.Count;

    public void InitializeDeck()
    {
        drawPile.Clear();
        discardPile.Clear();

        // Create Commander
        if (deckData.commander != null)
        {
            Commander = new RuntimeCard(
                deckData.commander,
                true
            );
            Commander.ChangeZone(CardZone.Hand);
        }
        else
        {
            Debug.LogError("Deck has no Commander assigned!");
        }

        // Create normal deck
        foreach (CardData cardData in deckData.cards)
        {
            if (cardData != null)
            {
                drawPile.Add(new RuntimeCard(cardData));
            }
        }

        Shuffle();

        Debug.Log(
            $"Deck initialized. " +
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
  

    public RuntimeCard DrawCard()
    {
        if (drawPile.Count == 0)
        {
            Debug.Log("Deck is empty.");
            return null;
        }

        RuntimeCard card = drawPile[0];
        drawPile.RemoveAt(0);

        card.ChangeZone(CardZone.Hand);

        return card;
    }

    public void Discard(RuntimeCard card)
    {
        if (card != null)
        {
            discardPile.Add(card);
        }
    }

    private void Shuffle()
    {
        for (int i = 0; i < drawPile.Count; i++)
        {
            int randomIndex = Random.Range(i, drawPile.Count);

            RuntimeCard temp = drawPile[i];
            drawPile[i] = drawPile[randomIndex];
            drawPile[randomIndex] = temp;
        }
    }
}