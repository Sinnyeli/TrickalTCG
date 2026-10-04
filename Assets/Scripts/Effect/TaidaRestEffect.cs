using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Taida Rest")]
public class TaidaRestEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets) => source?.SkipNextAttackTurn();
}
