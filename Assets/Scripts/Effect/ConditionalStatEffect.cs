using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ConditionalStatEffect", menuName = "Card Effects/ConditionalStat")]
public class ConditionalStatEffect : CardEffect
{
    public enum Condition { Damaged, AnotherFriendlyUnit }
    [SerializeField] private Condition condition = Condition.Damaged;
    [SerializeField] private int attackAmount = 2;
    public int GetAttackContribution(RuntimeCard unit)
    {
        if (unit == null || unit.IsSilenced || unit.Zone != CardZone.Field || GameManager.Instance == null) return 0;
        bool active = condition == Condition.Damaged ? unit.DamageTaken > 0 :
            GameManager.Instance.GetBattlefield(unit.Owner)?.Minions.Count > 1;
        return active ? attackAmount : 0;
    }
    // Queried continuously by GetAttack; never stacks persistent modifiers.
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets) { }
}
