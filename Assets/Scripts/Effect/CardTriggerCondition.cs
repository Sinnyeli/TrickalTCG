using UnityEngine;

[CreateAssetMenu(
    fileName = "CardTriggerCondition",
    menuName = "Card Effects/Conditions/Card Trigger Condition"
)]
public class CardTriggerCondition : TriggerCondition
{
    [Header("Relationship")]
    [SerializeField]
    private TriggerRelationship relationship =
        TriggerRelationship.Any;

    [Header("Card Filter")]
    [SerializeField]
    private EffectTargetFilter targetFilter =
        EffectTargetFilter.None;

    [SerializeField]
    private CardRace cardRace;

    [SerializeField]
    private CardType cardType;

    [SerializeField]
    private CardData specificCard;

    [Header("Source")]
    [SerializeField]
    private bool excludeSelf;


    public override bool IsMet(
        RuntimeCard source,
        RuntimeCard triggerCard,
        RuntimeModifier modifier)
    {
        if (source == null ||
            triggerCard == null)
        {
            return false;
        }


        // =========================================
        // SELF
        // =========================================

        if (excludeSelf &&
            source == triggerCard)
        {
            return false;
        }


        // =========================================
        // RELATIONSHIP
        // =========================================

        switch (relationship)
        {
            case TriggerRelationship.Allied:

                if (source.Owner !=
                    triggerCard.Owner)
                {
                    return false;
                }

                break;


            case TriggerRelationship.Enemy:

                if (source.Owner ==
                    triggerCard.Owner)
                {
                    return false;
                }

                break;
        }


        // =========================================
        // CARD FILTER
        // =========================================

        switch (targetFilter)
        {
            case EffectTargetFilter.None:

                return true;


            case EffectTargetFilter.SpecificCard:

                return triggerCard.Data ==
                       specificCard;


            case EffectTargetFilter.CardRace:

                if (triggerCard.Data
                    is MinionData minion)
                {
                    return minion.cardRace ==
                           cardRace;
                }

                return false;


            case EffectTargetFilter.CardType:

                return MatchesCardType(
                    triggerCard.Data
                );
        }


        return false;
    }


    private bool MatchesCardType(
        CardData data)
    {
        if (data == null)
            return false;

        switch (cardType)
        {
            case CardType.Apostle:
                return data is ApostleData;

            case CardType.Monster:
                return data is MonsterData;

            case CardType.Spell:
                return data is SpellData;

            case CardType.Artifact:
                return data is ArtifactData;

            default:
                return false;
        }
    }
}