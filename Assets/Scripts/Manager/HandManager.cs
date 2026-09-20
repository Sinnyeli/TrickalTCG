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

    RefreshHandLayout();
    CreateCardView(card);

    return true;
}
    // =========================================================
    // ADD GENERATED CARD
    // =========================================================


public bool AddGeneratedCard(CardData cardData, PlayerSide owner)
{
    if (cardData == null)
        return false;

    RuntimeCard card = new RuntimeCard(cardData);

    card.SetOwner(owner);
    card.ChangeZone(CardZone.Hand);

    return AddCard(card);
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

    int cost = card.GetManaCost();

    if (!turnManager.CanSpendMana(card.Owner, cost))
    {
        Debug.Log(
            $"{card.Data.cardName} cannot be played. " +
            $"Not enough mana."
        );

        return false;
    }

    // Spell cards do not enter the Battlefield.
    if (card.Data is SpellData)
    {
        return PlaySpellFromHand(card);
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
        turnManager.SpendMana(
            card.Owner,
            cost
        );

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

        int cost = card.GetManaCost();

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

private bool PlaySpellFromHand(RuntimeCard card)
{
    if (card == null)
        return false;

    if (!(card.Data is SpellData spellData))
        return false;

    if (spellData.SpellEffects == null ||
        spellData.SpellEffects.Count == 0)
    {
        Debug.Log(
            $"{card.Data.cardName} cannot be cast. " +
            "It has no Spell Effects."
        );

        return false;
    }

    TurnManager turnManager =
        GameManager.Instance.TurnManager;

    if (turnManager == null)
        return false;

    int cost = card.GetManaCost();

    // Check whether any effect requires
    // manual target selection.
    foreach (CardEffect effect in spellData.SpellEffects)
    {
        if (effect == null)
            continue;

        switch (effect.TargetType)
        {
            case EffectTargetType.EnemyUnit:
            case EffectTargetType.FriendlyUnit:
            case EffectTargetType.AnyUnit:
            case EffectTargetType.AnyTarget:

                // Targeted spells are handled
                // by PlayTargetedSpellFromHand.
                return false;
        }
    }

    Debug.Log(
        $"{card.Owner} casts {card.Data.cardName}!"
    );

    // The spell leaves the hand BEFORE
    // its effects resolve.
    RemoveCardFromHand(card);

    GameManager.Instance
        .EffectManager
        .ResolveSpell(card);

    CompleteSpellCast(
        card,
        cost
    );

    return true;
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

private void CompleteSpellCast(
    RuntimeCard card,
    int cost)
{
    if (card == null)
        return;

    TurnManager turnManager =
        GameManager.Instance.TurnManager;

    if (turnManager == null)
        return;

    Debug.Log(
        $"{card.Data.cardName} finished casting."
    );
    
    GameManager.Instance
        .EffectManager
        .TriggerResonance(card.Owner);

    turnManager.SpendMana(
        card.Owner,
        cost
    );

    DeckManager deck =
        GameManager.Instance.GetDeck(card.Owner);

    if (deck != null)
    {
        deck.Discard(card);
    }
}

public bool PlayTargetedSpellFromHand(
    RuntimeCard card,
    RuntimeCard unitTarget)
{
    if (card == null || unitTarget == null)
        return false;

    if (!(card.Data is SpellData spellData))
        return false;

    if (spellData.SpellEffects == null ||
        spellData.SpellEffects.Count == 0)
        return false;

    TurnManager turnManager =
        GameManager.Instance.TurnManager;

    if (turnManager == null)
        return false;

    if (!turnManager.IsMyTurn(card.Owner))
        return false;

    int cost = card.GetManaCost();

    if (!turnManager.CanSpendMana(
            card.Owner,
            cost))
        return false;

    // =========================================
    // VALIDATE TARGETED EFFECTS FIRST
    // =========================================

    bool hasUnitTargetEffect = false;

    foreach (CardEffect effect
             in spellData.SpellEffects)
    {
        if (effect == null)
            continue;

        switch (effect.TargetType)
        {
            case EffectTargetType.EnemyUnit:

                hasUnitTargetEffect = true;

                if (unitTarget.Owner == card.Owner)
                    return false;

                if (unitTarget.IsStealthed)
                    return false;

                if (!effect.MatchesTargetFilter(unitTarget))
                    return false;

                break;

            case EffectTargetType.FriendlyUnit:

                hasUnitTargetEffect = true;

                if (unitTarget.Owner != card.Owner)
                    return false;

                if (unitTarget.IsStealthed)
                    return false;

                if (!effect.MatchesTargetFilter(unitTarget))
                    return false;

                break;

            case EffectTargetType.AnyUnit:

                hasUnitTargetEffect = true;

                if (unitTarget.IsStealthed)
                    return false;

                if (!effect.MatchesTargetFilter(unitTarget))
                    return false;

                break;

            case EffectTargetType.AnyTarget:

                hasUnitTargetEffect = true;

                if (unitTarget.IsStealthed)
                    return false;

                if (!effect.MatchesTargetFilter(unitTarget))
                    return false;

                break;
        }
    }

    // This spell doesn't actually have an effect
    // that can target a unit.
    if (!hasUnitTargetEffect)
        return false;

    Debug.Log(
        $"{card.Owner} casts " +
        $"{card.Data.cardName} on " +
        $"{unitTarget.Data.cardName}."
    );

    // All validation succeeded.
    // The spell leaves the hand before resolving.
    RemoveCardFromHand(card);

    List<RuntimeCard> targets =
        new List<RuntimeCard>
        {
            unitTarget
        };

    // =========================================
    // RESOLVE EVERY SPELL EFFECT
    // =========================================

    foreach (CardEffect effect
             in spellData.SpellEffects)
    {
        if (effect == null)
            continue;

        switch (effect.TargetType)
        {
            // These use the manually selected unit.
            case EffectTargetType.EnemyUnit:
            case EffectTargetType.FriendlyUnit:
            case EffectTargetType.AnyUnit:
            case EffectTargetType.AnyTarget:

                effect.Resolve(
                    card,
                    targets
                );

                break;

            // Everything else uses the normal
            // EffectManager targeting pipeline.
            default:

                GameManager.Instance
                    .EffectManager
                    .ResolveCardEffect(
                        card,
                        effect
                    );

                break;
        }
    }

    CompleteSpellCast(
        card,
        cost
    );

    return true;
}
public bool PlayTargetedSpellFromHand(
    RuntimeCard card,
    PlayerView heroTarget)
{
    if (card == null || heroTarget == null)
        return false;

    if (!(card.Data is SpellData spellData))
        return false;

    if (spellData.SpellEffects == null ||
        spellData.SpellEffects.Count == 0)
        return false;

    TurnManager turnManager =
        GameManager.Instance.TurnManager;

    if (turnManager == null)
        return false;

    if (!turnManager.IsMyTurn(card.Owner))
        return false;

    int cost = card.GetManaCost();

    if (!turnManager.CanSpendMana(
            card.Owner,
            cost))
        return false;

    // =========================================
    // VALIDATE HERO-TARGETED EFFECTS
    // =========================================

    bool hasHeroTargetEffect = false;

    foreach (CardEffect effect
             in spellData.SpellEffects)
    {
        if (effect == null)
            continue;

        switch (effect.TargetType)
        {
            case EffectTargetType.EnemyHero:

                hasHeroTargetEffect = true;

                if (heroTarget.Side == card.Owner)
                    return false;

                break;

            case EffectTargetType.FriendlyHero:

                hasHeroTargetEffect = true;

                if (heroTarget.Side != card.Owner)
                    return false;

                break;

            case EffectTargetType.AnyTarget:

                hasHeroTargetEffect = true;
                break;
        }
    }

    if (!hasHeroTargetEffect)
        return false;

    Debug.Log(
        $"{card.Owner} casts " +
        $"{card.Data.cardName} on " +
        $"{heroTarget.Side} Hero."
    );

    // Spell is now committed.
    RemoveCardFromHand(card);

    // =========================================
    // RESOLVE EVERY SPELL EFFECT
    // =========================================

    foreach (CardEffect effect
             in spellData.SpellEffects)
    {
        if (effect == null)
            continue;

        switch (effect.TargetType)
        {
            case EffectTargetType.EnemyHero:
            case EffectTargetType.FriendlyHero:
            case EffectTargetType.AnyTarget:

                ResolveSpellEffectOnHero(
                    card,
                    effect,
                    heroTarget
                );

                break;

            default:

                GameManager.Instance
                    .EffectManager
                    .ResolveCardEffect(
                        card,
                        effect
                    );

                break;
        }
    }

    CompleteSpellCast(
        card,
        cost
    );

    return true;
}

public void RefreshCardView(RuntimeCard card)
{
    if (card == null)
        return;

    CardView view =
        FindCardView(card);

    if (view == null)
        return;

    view.RefreshCardView();
}

public void RefreshAllCardViews()
{
    foreach (Transform child in handContainer)
    {
        CardView view =
            child.GetComponent<CardView>();

        if (view == null)
            continue;

        view.RefreshCardView();
    }
}

public void TestReduceFirstCardCost()
{
    RuntimeCard card =
        GetFirstNormalCard();

    if (card == null)
        return;

    Debug.Log(
        $"Before: {card.GetManaCost()}"
    );

    card.ModifyManaCost(-2);

    Debug.Log(
        $"After: {card.GetManaCost()}"
    );

    RefreshCardView(card);
}
private void ResolveSpellEffectOnHero(
    RuntimeCard card,
    CardEffect effect,
    PlayerView heroTarget)
{
    if (effect is DamageEffect damageEffect)
    {
        damageEffect.ResolveHero(
            card,
            heroTarget
        );

        return;
    }

    if (effect is HealEffect healEffect)
    {
        healEffect.ResolveHero(
            card,
            heroTarget
        );

        return;
    }

    Debug.LogWarning(
        $"{effect.name} does not support Hero targeting."
    );
}

}