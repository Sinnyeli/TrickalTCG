using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AnnetteForcedAttackEffect", menuName = "Card Effects/Annette/Force Friendly Attack")]
public class AnnetteForcedAttackEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || targets == null || GameManager.Instance == null) return;
        var game = GameManager.Instance;
        var enemy = game.GetBattlefield(source.Owner == PlayerSide.Player ? PlayerSide.Opponent : PlayerSide.Player);
        if (enemy == null) return;
        foreach (var attacker in targets)
        {
            if (attacker == null || attacker == source || attacker.Owner != source.Owner || attacker.IsFrozen ||
                attacker.Zone != CardZone.Field || attacker.CurrentHealth <= 0) continue;
            var candidates = new List<RuntimeCard>(enemy.Minions).FindAll(c => c != null && c.Zone == CardZone.Field && c.CurrentHealth > 0 && !c.IsStealthed);
            if (candidates.Exists(c => c.HasKeyword(CardKeyword.Taunt))) candidates.RemoveAll(c => !c.HasKeyword(CardKeyword.Taunt));
            if (candidates.Count == 0) continue;
            game.CombatManager.ForceUnitAttack(attacker, candidates[Random.Range(0, candidates.Count)]);
        }
    }
}
