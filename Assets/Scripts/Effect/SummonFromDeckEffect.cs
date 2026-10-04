using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SummonFromDeckEffect", menuName = "Card Effects/SummonFromDeck")]
public class SummonFromDeckEffect : CardEffect
{
    [Min(1), SerializeField] private int amount = 3;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || GameManager.Instance == null) return;
        var game = GameManager.Instance;
        var deck = game.GetDeck(source.Owner);
        var field = game.GetBattlefield(source.Owner);
        if (deck == null || field == null) return;
        for (int i = 0; i < Mathf.Max(0, amount); i++)
        {
            if (!field.HasAvailableMinionSlot) break;
            var card = deck.DrawRandomMatchingCard(c => c != null && !c.IsCommander &&
                (c.Data is ApostleData || c.Data is MonsterData) && c.Data is MinionData minion &&
                minion.health > 0 && MatchesTargetFilter(c));
            if (card == null) break;
            if (!field.PlayCard(card))
            {
                deck.ReturnDrawnCardToDeck(card);
                break;
            }
        }
    }
}
