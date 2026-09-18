using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CostModifierEffect",
    menuName = "Card Effects/Cost Modifier"
)]
public class CostModifierEffect : CardEffect
{
    [Header("Cost Modification")]
    [SerializeField] private int amount = -1;

    [Header("Hand Selection")]
    [SerializeField]
    private HandCardSelection selection =
        HandCardSelection.AllMatching;

    public override void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets)
    {
        if (source == null)
            return;

        HandManager handManager =
            GameManager.Instance.GetHandManager(
                source.Owner
            );

        if (handManager == null)
            return;

        List<RuntimeCard> validCards =
            new List<RuntimeCard>();

        foreach (RuntimeCard card in handManager.Hand)
        {
            if (card == null)
                continue;

            if (!MatchesTargetFilter(card))
                continue;

            validCards.Add(card);
        }

        if (validCards.Count == 0)
            return;

        switch (selection)
        {
            case HandCardSelection.AllMatching:
                ModifyAll(
                    handManager,
                    validCards
                );
                break;

            case HandCardSelection.RandomMatching:
                ModifyRandom(
                    handManager,
                    validCards
                );
                break;
        }
    }

    private void ModifyAll(
        HandManager handManager,
        List<RuntimeCard> cards)
    {
        foreach (RuntimeCard card in cards)
        {
            ModifyCard(
                handManager,
                card
            );
        }
    }

    private void ModifyRandom(
        HandManager handManager,
        List<RuntimeCard> cards)
    {
        int index =
            Random.Range(0, cards.Count);

        RuntimeCard selected =
            cards[index];

        ModifyCard(
            handManager,
            selected
        );
    }

    private void ModifyCard(
        HandManager handManager,
        RuntimeCard card)
    {
        if (card == null)
            return;

        int oldCost =
            card.GetManaCost();

        card.ModifyManaCost(amount);

        int newCost =
            card.GetManaCost();

        handManager.RefreshCardView(card);

        Debug.Log(
            $"{card.Data.cardName} cost changed " +
            $"from {oldCost} to {newCost}."
        );
    }
}