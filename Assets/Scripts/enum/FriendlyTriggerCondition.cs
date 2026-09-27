using UnityEngine;

[CreateAssetMenu(
    fileName = "FriendlyTriggerCondition",
    menuName =
        "Card Effects/Trigger Conditions/Friendly"
)]
public class FriendlyTriggerCondition
    : TriggerCondition
{
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

        return source.Owner ==
               triggerCard.Owner;
    }
}