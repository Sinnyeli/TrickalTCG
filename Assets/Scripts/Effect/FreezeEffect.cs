using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FreezeEffect", menuName = "Card Effects/Freeze")]
public class FreezeEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || targets == null) return;
        foreach (var target in targets)
            if (target != null && target.Zone == CardZone.Field && target.CurrentHealth > 0)
                target.ApplyFreeze();
    }
}
