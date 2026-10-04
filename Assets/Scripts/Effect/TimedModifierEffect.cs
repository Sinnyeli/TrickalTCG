using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TimedModifierEffect", menuName = "Card Effects/TimedModifier")]
public class TimedModifierEffect : CardEffect
{
    [SerializeField] private int attackAmount = 1;
    [SerializeField] private int healthAmount = 1;
    [Min(1), SerializeField] private int turnEnds = 1;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || targets == null || GameManager.Instance == null) return;
        foreach (var target in new List<RuntimeCard>(targets))
        {
            if (target == null || target.Zone != CardZone.Field || target.CurrentHealth <= 0) continue;
            target.AddTimedModifier(new RuntimeModifier(attackAmount, healthAmount, true, source), Mathf.Max(1, turnEnds));
            GameManager.Instance.GetBattlefield(target.Owner)?.RefreshMinionView(target);
            GameManager.Instance.CombatManager.CheckDeath(target);
        }
    }
}
