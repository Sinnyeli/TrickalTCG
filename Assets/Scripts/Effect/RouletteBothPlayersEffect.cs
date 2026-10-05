using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Roulette Both Players")]
public class RouletteBothPlayersEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || GameManager.Instance == null) return;
        var game = GameManager.Instance;
        int outcome = Random.Range(0, 4); // One shared roll: each branch is exactly 25%, never rerolled for eligibility.
        Debug.Log("Roulette shared outcome: " + outcome + " (0 draw, 1 destroy, 2 summon, 3 discard)");
        if (outcome == 1)
        {
            var victims = new List<RuntimeCard>();
            foreach (var side in new[] { PlayerSide.Player, PlayerSide.Opponent })
            { var field = game.GetBattlefield(side); if (field != null) victims.AddRange(field.Minions); }
            foreach (var unit in victims)
            { if (unit.Zone != CardZone.Field) continue; unit.Kill(source); game.CombatManager.CheckDeath(unit); }
            return;
        }
        foreach (var side in new[] { PlayerSide.Player, PlayerSide.Opponent })
        {
            if (game.IsGameOver) break;
            if (outcome == 0) { game.DrawCard(side); continue; }
            var deck = game.GetDeck(side);
            if (deck == null) continue;
            if (outcome == 2)
            {
                var field = game.GetBattlefield(side);
                if (field == null || !field.HasAvailableMinionSlot) continue;
                var card = deck.DrawRandomMatchingCard(c => c != null && !c.IsCommander && c.Data is MinionData data && data.health > 0);
                if (card != null && !field.PlayCard(card)) deck.ReturnDrawnCardToDeck(card);
            }
            else
            {
                var hand = game.GetHandManager(side); if (hand == null) continue;
                var eligible = new List<RuntimeCard>(hand.Hand).FindAll(c => c != null && !c.IsCommander && c.Zone == CardZone.Hand);
                if (eligible.Count == 0) continue;
                var card = eligible[Random.Range(0, eligible.Count)];
                if (hand.RemoveCardFromHand(card)) deck.Discard(card);
            }
        }
    }
}
