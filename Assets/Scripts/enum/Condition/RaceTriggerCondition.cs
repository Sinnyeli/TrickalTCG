using UnityEngine;

[CreateAssetMenu(
    fileName = "RaceTriggerCondition",
    menuName =
        "Card Effects/Trigger Conditions/Card Race"
)]
public class RaceTriggerCondition
    : TriggerCondition
{
    [SerializeField]
    private CardRace requiredRace;


    public override bool IsMet(
        RuntimeCard source,
        RuntimeCard triggerCard,
        RuntimeModifier modifier)
    {
        if (triggerCard == null ||
            triggerCard.Data == null)
        {
            return false;
        }


        if (triggerCard.Data
            is ApostleData apostle)
        {
            return apostle.HasRace(requiredRace);
        }


        if (triggerCard.Data
            is MonsterData monster)
        {
            return monster.HasRace(requiredRace);
        }


        return false;
    }
}