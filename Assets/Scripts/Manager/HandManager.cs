using System.Collections.Generic;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    [Header("Hand Settings")]
    [SerializeField] private int maxHandSize = 7;

    [Header("UI")]
    [SerializeField] private Transform handContainer;
    [SerializeField] private Transform commanderContainer;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private HandLayout handLayout;

    private List<RuntimeCard> hand = new List<RuntimeCard>();

    public IReadOnlyList<RuntimeCard> Hand => hand;

    public int HandSize => NormalCardCount();

    // =========================================================
    // ADD CARD
    // =========================================================

    public bool AddCard(RuntimeCard card)
    {
        if (card == null)
            return false;

        // If somehow a Commander is passed through AddCard,
        // send it to the Commander system instead.
        if (card.IsCommander)
        {
            AddCommander(card);
            return true;
        }

        // Check normal hand limit
        if (NormalCardCount() >= maxHandSize)
        {
            Debug.Log(
                $"Hand full! {card.Data.cardName} was exhausted."
            );

            // Eventually this should go to an Exhaust/Discard system.
            return false;
        }

        hand.Add(card);

        CreateCardView(card);

        return true;
    }

    // =========================================================
    // ADD COMMANDER
    // =========================================================
    public void AddCommander(RuntimeCard commander)
{
    if (commander == null)
        return;

    if (!commander.IsCommander)
    {
        Debug.LogError("This card is not a Commander!");
        return;
    }

    if (hand.Contains(commander))
    {
        Debug.Log("Commander is already in hand.");
        return;
    }

    hand.Add(commander);
    CreateCommanderView(commander);
}

    // =========================================================
    // CREATE NORMAL CARD UI
    // =========================================================

    private void CreateCardView(RuntimeCard card)
    {
        GameObject cardObject = Instantiate(
            cardPrefab,
            handContainer
        );

        CardView cardView =
            cardObject.GetComponent<CardView>();

        if (cardView != null)
        {
            cardView.SetCard(card.Data);
        }
        else
        {
            Debug.LogError(
                "Card Prefab is missing CardView component!"
            );
        }

        RefreshHandLayout();
    }

    // =========================================================
    // CREATE COMMANDER UI
    // =========================================================

    private void CreateCommanderView(RuntimeCard commander)
    {
        GameObject cardObject = Instantiate(
            cardPrefab,
            commanderContainer
        );

        CardView cardView =
            cardObject.GetComponent<CardView>();

        if (cardView != null)
        {
            cardView.SetCard(commander.Data);
        }
        else
        {
            Debug.LogError(
                "Card Prefab is missing CardView component!"
            );
        }
    }

    // =========================================================
    // HAND LAYOUT
    // =========================================================

    private void RefreshHandLayout()
    {
        if (handLayout != null)
        {
            handLayout.RefreshLayout();
        }
    }

    // =========================================================
    // HAND COUNT
    // =========================================================

    private int NormalCardCount()
    {
        int count = 0;

        foreach (RuntimeCard card in hand)
        {
            if (!card.IsCommander)
            {
                count++;
            }
        }

        return count;
    }

    // =========================================================
    // DRAW TEST
    // =========================================================

    public void DrawTestCard()
    {
        DeckManager deckManager =
            FindFirstObjectByType<DeckManager>();

        if (deckManager == null)
        {
            Debug.LogError(
                "DeckManager not found!"
            );

            return;
        }

        RuntimeCard card =
            deckManager.DrawCard();

        if (card == null)
            return;

        bool added = AddCard(card);

        // If the card couldn't be added because the hand
        // is full, discard/exhaust it.
        if (!added)
        {
            deckManager.Discard(card);
        }
    }

    // =========================================================
    // REMOVE CARD
    // =========================================================

    public bool RemoveCard(RuntimeCard card)
    {
        if (card == null)
            return false;

        if (!hand.Contains(card))
            return false;

        hand.Remove(card);

        RefreshHandLayout();

        return true;
    }

    // =========================================================
    // RETURN COMMANDER TO HAND
    // =========================================================

    public void ReturnCommanderToHand(RuntimeCard commander)
    {
        if (commander == null)
            return;

        if (!commander.IsCommander)
        {
            Debug.LogError(
                "Attempted to return a non-Commander card " +
                "as Commander."
            );

            return;
        }

        if (!(commander.Data is ApostleData))
        {
            Debug.LogError(
                "Only Apostles can be Commanders."
            );

            return;
        }

        // Make sure it isn't already in hand.
        if (hand.Contains(commander))
            return;

        hand.Add(commander);

        CreateCommanderView(commander);
    }
}