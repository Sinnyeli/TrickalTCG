using System.Collections.Generic;
using UnityEngine;

public abstract class CardEffect : ScriptableObject
{
  [Header("Targeting")]
    [SerializeField] private EffectTargetType targetType;

    [Header("Target Filter")]
    [SerializeField] private EffectTargetFilter targetFilter;

    [SerializeField] private CardRace cardRace;
    public CardRace CardRace => cardRace;

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

        default:
            return false;
    }
}
}