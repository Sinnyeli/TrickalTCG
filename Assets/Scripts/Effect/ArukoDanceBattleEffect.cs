using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ArukoDanceBattleEffect", menuName = "Card Effects/Elemental/ArukoDanceBattleEffect")]
public class ArukoDanceBattleEffect : CardEffect
{
    private bool resolving;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || GameManager.Instance == null || resolving) return;
        var game = GameManager.Instance;
        var attackers = new List<RuntimeCard>();
        foreach (var side in new[] { source.Owner, source.Owner == PlayerSide.Player ? PlayerSide.Opponent : PlayerSide.Player })
        {
            var field = game.GetBattlefield(side); if (field != null) attackers.AddRange(field.Minions);
        }
        resolving = true;
        try
        {
            foreach (var attacker in attackers)
            {
                if (game.IsGameOver) break;
                if (attacker == null || attacker.Zone != CardZone.Field || attacker.CurrentHealth <= 0 || attacker.IsFrozen) continue;
                var candidates = new List<RuntimeCard>();
                foreach (var side in new[] { PlayerSide.Player, PlayerSide.Opponent })
                {
                    var field = game.GetBattlefield(side); if (field != null) candidates.AddRange(field.Minions);
                }
                candidates.RemoveAll(c => c == null || c == attacker || c.Zone != CardZone.Field || c.CurrentHealth <= 0 || c.IsStealthed);
                bool enemyTaunt = candidates.Exists(c => c.Owner != attacker.Owner && c.HasKeyword(CardKeyword.Taunt));
                if (enemyTaunt) candidates.RemoveAll(c => c.Owner != attacker.Owner && !c.HasKeyword(CardKeyword.Taunt));
                if (candidates.Count > 0) game.CombatManager.ForceUnitAttack(attacker, candidates[Random.Range(0, candidates.Count)], true);
            }
        }
        finally { resolving = false; }
    }
}
