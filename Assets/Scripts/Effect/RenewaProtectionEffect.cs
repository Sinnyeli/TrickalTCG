using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Renewa Protection")]
public class RenewaProtectionEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        foreach (var target in targets) if (target != null) target.ProtectUntilTurnEnd();
    }
}
