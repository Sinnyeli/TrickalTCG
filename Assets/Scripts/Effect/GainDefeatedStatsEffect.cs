using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Gain Defeated Stats")]
public class GainDefeatedStatsEffect : CardEffect
{
    public void ResolveSnapshot(RuntimeCard source, RuntimeModifier snapshot)
    {
        if (source == null || snapshot == null || source.Zone != CardZone.Field || source.CurrentHealth <= 0) return;
        source.AddModifier(new RuntimeModifier(snapshot.AttackBonus, snapshot.HealthBonus, true, source));
        GameManager.Instance?.GetBattlefield(source.Owner)?.RefreshMinionView(source);
    }
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (targets == null || targets.Count == 0 || targets[0] == null) return;
        ResolveSnapshot(source, new RuntimeModifier(targets[0].GetAttack(), targets[0].GetMaxHealth(), false));
    }
}
