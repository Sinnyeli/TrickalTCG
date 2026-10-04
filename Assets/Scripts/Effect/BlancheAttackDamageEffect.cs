using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlancheAttackDamageEffect", menuName = "Card Effects/Elemental/BlancheAttackDamageEffect")]
public class BlancheAttackDamageEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || targets == null || GameManager.Instance == null) return;
        var game = GameManager.Instance;
        foreach (var target in new List<RuntimeCard>(targets))
        {
            if (target == null || target.Zone != CardZone.Field || target.CurrentHealth <= 0) continue;
            int amount = target.GetAttack();
            bool damaged = target.TakeDamage(amount, source);
            if (damaged && target.CurrentHealth > 0) game.EffectManager.ResolveOnDamageTaken(target);
            game.GetBattlefield(target.Owner)?.RefreshMinionView(target);
            game.CombatManager.CheckDeath(target);
        }
    }
}
