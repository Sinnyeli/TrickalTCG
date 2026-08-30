using System.Collections.Generic;
using UnityEngine;

public class HandManager : MonoBehaviour
{
    [Header("Game Manager")]
    [SerializeField] private GameManager gameManager;

    [Header("Hand Settings")]
    [SerializeField] private int maxHandSize = 7;

    [Header("UI")]
    [SerializeField] public Transform handContainer;
    [SerializeField] private Transform commanderContainer;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private HandLayout handLayout;
    [SerializeField] private HandLayout commanderHandLayout;

    [SerializeField]
    private PlayerSide owner;

    public Transform HandContainer => handContainer;
    // Runtime card. Essentially represent card hand is holding.
    private RuntimeCard runtimeCard;
    public RuntimeCard RuntimeCard => runtimeCard;
    private List<RuntimeCard> hand = new List<RuntimeCard>();
    // This holds the list of cards in hand.

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
    // Add Commander to hand 
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
         GameObject obj =
        Instantiate(cardPrefab, handContainer);

        CardviewBase view =
            obj.GetComponent<CardviewBase>();


        if (view != null)
        {
            view.SetCard(card);
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

        }
    // =========================================================
    // CREATE COMMANDER UI
    // =========================================================

    private void CreateCommanderView(RuntimeCard commander)
    {
        GameObject obj = Instantiate(
            cardPrefab,
            commanderContainer
        );
        CardviewBase view =
                    obj.GetComponent<CardviewBase>();


        if (view != null)
        {
            view.SetCard(commander);
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
    // Hand Layout Refresh
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
    // Hand count discounting commander
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
    // DRAW TEST (used with button OnClick())
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
    // Remove Card Here
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
    // Play Card from Hand here. 
    // =========================================================

  
public bool PlayCardFromHand(RuntimeCard card)
{
    if (card == null)
        return false;

    if (card.IsCommander)
    {
        PlayCommanderFromHand(card);
        return false;
    }

    /// Turn Manager and Mana
    TurnManager turnManager =
        GameManager.Instance.TurnManager;

    if (turnManager == null)
        return false;

    if (!turnManager.IsMyTurn(card.Owner))
    {
        Debug.Log(
            $"{card.Data.cardName} cannot be played. " +
            $"It is not {card.Owner}'s turn."
        );

        return false;
    }

    int cost = card.Data.manaCost;

    if (!turnManager.CanSpendMana(card.Owner, cost))
    {
        Debug.Log(
            $"{card.Data.cardName} cannot be played. " +
            $"Not enough mana."
        );

        return false;
    }



    // Playing Card on Battlefield
    BattlefieldManager battlefield = GameManager.Instance.GetBattlefield(card.Owner);

    if (battlefield == null)
        return false;

    bool success =
        battlefield.PlayCard(card);

    if (!success)
        return false;
   // Only spend mana after the card successfully enters the field.
    turnManager.SpendMana(card.Owner, cost);

    RemoveCardFromHand(card);

    return true;
}

    public bool PlayCommanderFromHand(RuntimeCard card)
    {
        if (card == null)
            return false;

        if (!card.IsCommander)
        {
            Debug.LogWarning(
                $"{card.Data.cardName} is not a Commander."
            );

            return false;
        }

                /// Turn Manager and Mana
        TurnManager turnManager =
            GameManager.Instance.TurnManager;

        if (turnManager == null)
            return false;

        if (!turnManager.IsMyTurn(card.Owner))
        {
        Debug.Log(
            $"{card.Data.cardName} cannot be played. " +
            $"It is not {card.Owner}'s turn."
        );

        return false;
        }

        int cost = card.Data.manaCost;

        if (!turnManager.CanSpendMana(card.Owner, cost))
        {
            Debug.Log(
                $"{card.Data.cardName} cannot be played. " +
                $"Not enough mana."
            );

            return false;
        }

        BattlefieldManager battlefield = GameManager.Instance.GetBattlefield(card.Owner);

        if (battlefield == null)
        {
            Debug.LogError(
                $"No BattlefieldManager found for {card.Owner}."
            );

            return false;
        }

        bool success =
            battlefield.PlayCard(card);

        if (!success)
            return false;
    // Only spend mana after the card successfully enters the field.
        turnManager.SpendMana(card.Owner, cost);

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

// Test Area

public RuntimeCard GetFirstNormalCard()
{
    foreach (RuntimeCard card in hand)
    {
        if (card == null)
            continue;

        if (!card.IsCommander)
            return card;
    }

    return null;
}



}