using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Full Heal", fileName = "FullHealEffect")]
public class FullHealEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || targets == null || GameManager.Instance == null) return;
        foreach (RuntimeCard target in targets)
        {
            if (target == null || target.Zone != CardZone.Field || target.CurrentHealth <= 0) continue;
            target.Heal(Mathf.Max(0, target.GetMaxHealth() - target.CurrentHealth));
            GameManager.Instance.GetBattlefield(target.Owner)?.RefreshMinionView(target);
        }
    }
}
