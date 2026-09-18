using System.Collections.Generic;
using UnityEngine;

public abstract class CardEffect : ScriptableObject
{
  [Header("Targeting")]
    [SerializeField] private EffectTargetType targetType;

    [Header("Target Filter")]
    [SerializeField] private EffectTargetFilter targetFilter;

    [SerializeField] private CardRace cardRace;
    [SerializeField] private CardType cardType;


    public CardRace CardRace => cardRace;
    public CardType CardType => cardType;

    [SerializeField] private CardData specificCard;

    public EffectTargetType TargetType => targetType;

    public EffectTargetFilter TargetFilter => targetFilter;

    

    public CardData SpecificCard => specificCard;

    public abstract void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets
    );

    public bool MatchesTargetFilter(RuntimeCard target)
{
    if (target == null)
        return false;

    switch (targetFilter)
    {
        case EffectTargetFilter.None:
            return true;

        case EffectTargetFilter.SpecificCard:
            return target.Data == specificCard;

        case EffectTargetFilter.CardRace:
            if (target.Data is MonsterData monster)
                return monster.cardRace == cardRace;

            if (target.Data is ApostleData apostle)
                return apostle.cardRace == cardRace;

            return false;

        case EffectTargetFilter.CardType:
            return MatchesCardType(
                target.Data,
                cardType
            );

        default:
            return false;
    }

}

    private bool MatchesCardType(
        CardData data,
        CardType type)
    {
        if (data == null)
            return false;

        switch (type)
        {
            case CardType.Monster:
                return data is MonsterData;

            case CardType.Apostle:
                return data is ApostleData;

            case CardType.Spell:
                return data is SpellData;

            case CardType.Artifact:
                return data is ArtifactData;

            default:
                return false;
        }
    }
}