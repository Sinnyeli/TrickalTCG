using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SummonEffect",
    menuName = "Card Effects/Summon"
)]
public class SummonEffect : CardEffect
{
    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Summon Location")]
    [Tooltip(
        "Determines which side of the battlefield " +
        "receives the summoned creatures."
    )]
    [SerializeField]
    private SummonSideMode summonSideMode =
        SummonSideMode.SourceOwner;


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

    [Tooltip(
        "Selected summons cards from the list in order. " +
        "Random chooses randomly from the list."
    )]
    [SerializeField]
    private SummonSelectionType selectionType =
        SummonSelectionType.Selected;


    // =========================================================
    // RESOLVE
    // =========================================================

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
                $"{source.Data.cardName}: " +
                $"SummonEffect has no cards in its summon pool."
            );

            return;
        }

        PlayerSide summonSide =
            GetSummonSide(source);

        BattlefieldManager battlefield =
            GameManager.Instance.GetBattlefield(
                summonSide
            );

        if (battlefield == null)
        {
            Debug.LogWarning(
                $"Could not find battlefield for " +
                $"{summonSide}."
            );

            return;
        }

        switch (selectionType)
        {
            case SummonSelectionType.Selected:

                SummonSelected(
                    source,
                    battlefield,
                    summonSide
                );

                break;


            case SummonSelectionType.Random:

                SummonRandom(
                    source,
                    battlefield,
                    summonSide
                );

                break;
        }
    }


    // =========================================================
    // SELECTED
    // =========================================================

    private void SummonSelected(
        RuntimeCard source,
        BattlefieldManager battlefield,
        PlayerSide summonSide)
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
                summonSide,
                cardData
            );
        }
    }


    // =========================================================
    // RANDOM
    // =========================================================

    private void SummonRandom(
        RuntimeCard source,
        BattlefieldManager battlefield,
        PlayerSide summonSide)
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
                summonSide,
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
        PlayerSide summonSide,
        CardData cardData)
    {
        if (cardData == null)
            return;

        // Only minions can be summoned
        // onto the battlefield.
        if (!(cardData is MinionData))
        {
            Debug.LogWarning(
                $"{cardData.cardName} cannot be summoned. " +
                $"Only minions can be summoned."
            );

            return;
        }

        RuntimeCard summonedCard =
            new RuntimeCard(cardData);

        summonedCard.SetOwner(
            summonSide
        );

        /*
         * BattlefieldManager.PlayCard currently expects
         * cards to originate from Hand.
         *
         * We can replace this later with a dedicated
         * BattlefieldManager.SummonCard method.
         */
        summonedCard.ChangeZone(
            CardZone.Hand
        );

        bool summoned =
            battlefield.PlayCard(
                summonedCard
            );

        if (!summoned)
        {
            Debug.Log(
                $"{source.Data.cardName} failed to summon " +
                $"{cardData.cardName}."
            );

            return;
        }

        Debug.Log(
            $"{source.Data.cardName} summoned " +
            $"{cardData.cardName} onto " +
            $"{summonSide}'s battlefield."
        );
    }


    // =========================================================
    // SUMMON SIDE
    // =========================================================

    private PlayerSide GetSummonSide(
        RuntimeCard source)
    {
        switch (summonSideMode)
        {
            case SummonSideMode.Player:
                return PlayerSide.Player;

            case SummonSideMode.Opponent:
                return PlayerSide.Opponent;

            case SummonSideMode.SourceOwner:
            default:
                return source.Owner;
        }
    }
}