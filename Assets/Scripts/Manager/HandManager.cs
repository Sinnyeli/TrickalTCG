using System.Collections.Generic;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    [Header("Hand Settings")]
    [SerializeField] private int maxHandSize = 7;

    [Header("UI")]
    [SerializeField] public Transform handContainer;
    [SerializeField] private Transform commanderContainer;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private HandLayout handLayout;
    [SerializeField] private HandLayout commanderHandLayout;

    public Transform HandContainer => handContainer;
    // Runtime card. Essentially represent card hand is holding.
    private RuntimeCard runtimeCard;
    public RuntimeCard RuntimeCard => runtimeCard;
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

    if (card.Zone != CardZone.Hand)
    {
        Debug.LogWarning(
            $"{card.Data.cardName} cannot be added to Hand. " +
            $"Current Zone: {card.Zone}"
        );

        return false;
    }

    if (card.IsCommander)
    {
        AddCommander(card);
        return true;
    }

    if (NormalCardCount() >= maxHandSize)
    {
        Debug.Log(
            $"Hand full! {card.Data.cardName} was exhausted."
        );

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
            cardView.SetCard(card);
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
    // Set Card UI
    // =========================================================

    public void SetCard(RuntimeCard card)
        {
            runtimeCard = card;

            // Display the card's data
            // Keep whatever UI code you already have here.
            //
            // Example:
            // nameText.text = card.Data.cardName;
            // descriptionText.text = card.Data.description;
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
            cardView.SetCard(commander);
        }
        else
        {
            Debug.LogError(
                "Card Prefab is missing CardView component!"
            );
        }
    }
    // =========================================================
    // Destroy COMMANDER UI
    // =========================================================

    private void DestroyCommanderView(RuntimeCard commander)
{
    foreach (Transform child in commanderContainer)
    {
        CardView cardView =
            child.GetComponent<CardView>();

        if (cardView == null)
            continue;

        if (cardView.runtimeCard == commander)
        {
            Destroy(cardView.gameObject);
            return;
        }
    }
}

    // =========================================================
    // HAND LAYOUT
    // =========================================================

    public void RefreshHandLayout()
    {
        if (handLayout != null)
        {
            handLayout.RefreshLayout();
        }
    }
    public void RefreshCommanderLayout()
    {
    if (commanderHandLayout != null)
        commanderHandLayout.RefreshLayout();
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
    // PlayCardFromHand
    // =========================================================

  
public bool PlayCardFromHand(RuntimeCard card)
{
    
    if (card == null)
        return false;

    BattlefieldManager battlefield =
        GameManager.Instance.playerBattlefieldManager;

    if (battlefield == null)
        return false;

    bool success =
        battlefield.PlayCard(card);

    if (!success)
        return false;

    // Remove from Hand.
    RemoveCardFromHand(card);

    return true;
}


    // =========================================================
    // Find card from hand. 
    // =========================================================

private CardView FindCardView(RuntimeCard card)
{
    foreach (Transform child in handContainer)
    {
        CardView cardView =
            child.GetComponent<CardView>();

        if (cardView == null)
            continue;

        if (cardView.runtimeCard == card)
            return cardView;
    }

    return null;
}
    // =========================================================
    // Delete card from hand
    // =========================================================

public bool RemoveCardFromHand(RuntimeCard card)
{
    if (!hand.Contains(card))
        return false;

    hand.Remove(card);

    if (card.IsCommander)
    {
        DestroyCommanderView(card);
    }
    else
    {
        DestroyHandView(card);
    }

    RefreshHandLayout();

    return true;
}

private void DestroyHandView(RuntimeCard card)
{
    CardView cardView = FindCardView(card);

    if (cardView != null)
    {
        Destroy(cardView.gameObject);
    }
}
}