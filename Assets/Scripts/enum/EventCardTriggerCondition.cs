using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Trigger Conditions/Event Card")]
public class EventCardTriggerCondition : TriggerCondition
{
    public enum Relation { Any, Friendly, Enemy, Self, OtherFriendly }
    [SerializeField] private Relation relation;
    [SerializeField] private bool filterType;
    [SerializeField] private CardType cardType;
    [SerializeField] private CardData specificCard;
    public override bool IsMet(RuntimeCard source, RuntimeCard card, RuntimeModifier modifier)
    {
        if (source == null || card == null) return false;
        if (relation == Relation.Friendly && source.Owner != card.Owner ||
            relation == Relation.Enemy && source.Owner == card.Owner ||
            relation == Relation.Self && source != card ||
            relation == Relation.OtherFriendly && (source == card || source.Owner != card.Owner)) return false;
        if (specificCard != null && card.Data != specificCard) return false;
        return !filterType || (cardType == CardType.Apostle && card.Data is ApostleData) ||
            (cardType == CardType.Monster && card.Data is MonsterData) ||
            (cardType == CardType.Spell && card.Data is SpellData) ||
            (cardType == CardType.Artifact && card.Data is ArtifactData);
    }
}
