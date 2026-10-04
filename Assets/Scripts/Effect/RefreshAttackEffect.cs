using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RefreshAttackEffect", menuName = "Card Effects/RefreshAttack")]
public class RefreshAttackEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || targets == null) return;
        foreach (var target in targets)
            if (target != null && target.Zone == CardZone.Field && target.CurrentHealth > 0)
                target.EnableAttack(); // CanAttack still enforces Freeze and hero restrictions remain.
    }
}
