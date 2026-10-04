using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OpalDamagePenaltyEffect", menuName = "Card Effects/Opal/Extra Damage Taken")]
public class OpalDamagePenaltyEffect : CardEffect
{
    public int ExtraDamage => 1;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets) { }
}
