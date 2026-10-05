using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Destroy At Turn End")]
public class DestroyAtTurnEndEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || targets == null) return;
        foreach (var target in targets)
            if (target != null && target.Zone == CardZone.Field && target.CurrentHealth > 0)
                target.ScheduleDeathAtTurnEnd(source);
    }
}
