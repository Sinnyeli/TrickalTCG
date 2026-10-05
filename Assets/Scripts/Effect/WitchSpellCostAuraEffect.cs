using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Witch Spell Cost Aura")]
public class WitchSpellCostAuraEffect : CardEffect
{
    [SerializeField, Min(0)] private int amount = 1;
    public int Amount => amount;
    // Cost is queried dynamically: it disappears when the source leaves the field or is silenced.
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets) { }
}
