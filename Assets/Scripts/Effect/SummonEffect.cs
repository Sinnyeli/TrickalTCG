using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SummonEffect",
    menuName = "Card Effects/Summon"
)]
public class SummonEffect : CardEffect
{
    [Header("Summon Pool")]
    [Tooltip("Cards that can be summoned by this effect.")]
    [SerializeField]
    private List<CardData> summonPool =
        new List<CardData>();

    [Header("Summon Settings")]
    [Tooltip("How many creatures are summoned.")]
    [Min(1)]
    [SerializeField]
    private int amount = 1;

    [Tooltip("Selected summons cards from the list in order. Random chooses randomly from the list.")]
    [SerializeField]
    private SummonSelectionType selectionType =
        SummonSelectionType.Selected;

    [Tooltip("Which battlefield receives the summoned creatures.")]
    [SerializeField]
    private PlayerSide summonSide =
        PlayerSide.Player;

    public override void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets)
    {
        if (source == null)
            return;

        if (summonPool == null ||
            summonPool.Count == 0)
        {
            Debug.LogWarning(
                $"{source.Data.cardName}: SummonEffect has no cards in its summon pool."
            );

            return;
        }

        BattlefieldManager battlefield =
            GameManager.Instance.GetBattlefield(
                summonSide
            );

        if (battlefield == null)
        {
            Debug.LogWarning(
                $"Could not find battlefield for {summonSide}."
            );

            return;
        }

        switch (selectionType)
        {
            case SummonSelectionType.Selected:

                SummonSelected(
                    source,
                    battlefield
                );

                break;

            case SummonSelectionType.Random:

                SummonRandom(
                    source,
                    battlefield
                );

                break;
        }
    }

    // =========================================================
    // SELECTED
    // =========================================================

    private void SummonSelected(
        RuntimeCard source,
        BattlefieldManager battlefield)
    {
        int summonCount =
            Mathf.Min(
                amount,
                summonPool.Count
            );

        for (int i = 0; i < summonCount; i++)
        {
            CardData cardData =
                summonPool[i];

            SummonCard(
                source,
                battlefield,
                cardData
            );
        }
    }

    // =========================================================
    // RANDOM
    // =========================================================

    private void SummonRandom(
        RuntimeCard source,
        BattlefieldManager battlefield)
    {
        for (int i = 0; i < amount; i++)
        {
            int randomIndex =
                Random.Range(
                    0,
                    summonPool.Count
                );

            CardData selectedCard =
                summonPool[randomIndex];

            SummonCard(
                source,
                battlefield,
                selectedCard
            );
        }
    }

    // =========================================================
    // SUMMON CARD
    // =========================================================

    private void SummonCard(
        RuntimeCard source,
        BattlefieldManager battlefield,
        CardData cardData)
    {
        if (cardData == null)
            return;

        // Only Monsters and Apostles can exist
        // as minions on the battlefield.
        if (!(cardData is MonsterData) &&
            !(cardData is ApostleData))
        {
            Debug.LogWarning(
                $"{cardData.cardName} cannot be summoned. " +
                $"Only Monsters and Apostles can be summoned."
            );

            return;
        }

        RuntimeCard summonedCard =
            new RuntimeCard(cardData);

        // The summoned creature belongs to the
        // battlefield it was summoned onto.
        summonedCard.SetOwner(
            summonSide
        );

        /*
         * BattlefieldManager.PlayCard currently
         * expects cards to originate from Hand.
         *
         * This is temporary until we give
         * BattlefieldManager a dedicated
         * SummonCard method.
         */
        summonedCard.ChangeZone(
            CardZone.Hand
        );

        battlefield.PlayCard(
            summonedCard
        );

        Debug.Log(
            $"{source.Data.cardName} summoned " +
            $"{cardData.cardName} onto " +
            $"{summonSide}'s battlefield."
        );
    }
}