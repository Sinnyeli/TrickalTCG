using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "AlliedCardsOnFieldCondition",
    menuName =
        "Card Effects/Conditions/Allied Cards On Field"
)]
public class AlliedCardsOnFieldCondition
    : CardEffectCondition
{
    [SerializeField]
    private List<CardData> cards =
        new List<CardData>();

    [Tooltip(
        "ON = all listed cards are required. " +
        "OFF = at least one listed card is required."
    )]
    [SerializeField]
    private bool requireAll;


    public override bool IsMet(
        RuntimeCard source)
    {
        if (source == null)
            return false;

        if (cards == null ||
            cards.Count == 0)
        {
            return false;
        }


        BattlefieldManager battlefield =
            GameManager.Instance.GetBattlefield(
                source.Owner
            );

        if (battlefield == null)
            return false;


        // =========================================
        // ALL
        // =========================================

        if (requireAll)
        {
            foreach (CardData requiredCard
                     in cards)
            {
                if (requiredCard == null)
                    continue;

                bool found = false;


                foreach (RuntimeCard fieldCard
                         in battlefield.Minions)
                {
                    if (fieldCard == null)
                        continue;

                    if (fieldCard.Data ==
                        requiredCard)
                    {
                        found = true;
                        break;
                    }
                }


                if (!found)
                    return false;
            }


            return true;
        }


        // =========================================
        // ANY
        // =========================================

        foreach (RuntimeCard fieldCard
                 in battlefield.Minions)
        {
            if (fieldCard == null)
                continue;


            if (cards.Contains(
                    fieldCard.Data))
            {
                return true;
            }
        }


        return false;
    }
}