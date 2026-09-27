using UnityEngine;

[CreateAssetMenu(
    fileName = "NotSelfTriggerCondition",
    menuName =
        "Card Effects/Trigger Conditions/Not Self"
)]
public class NotSelfTriggerCondition
    : TriggerCondition
{
    public override bool IsMet(
        RuntimeCard source,
        RuntimeCard triggerCard,
        RuntimeModifier modifier)
    {
        return source != null &&
               triggerCard != null &&
               source != triggerCard;
    }
}