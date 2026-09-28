using UnityEngine;

[CreateAssetMenu(
    fileName = "DidNotAttackCondition",
    menuName =
        "Card Effects/Conditions/Did Not Attack"
)]
public class DidNotAttackCondition
    : CardEffectCondition
{
    public override bool IsMet(
        RuntimeCard source)
    {
        if (source == null)
            return false;

        return !source.HasAttackedThisTurn;
    }
}