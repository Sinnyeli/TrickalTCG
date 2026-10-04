using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ErpinCastBreadEffect", menuName = "Card Effects/Erpin/Cast Bread On Self")]
public class ErpinCastBreadEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        var game = GameManager.Instance;
        if (source == null || game == null) return;
        var hand = game.GetHandManager(source.Owner);
        if (hand == null) return;
        var breads = new List<RuntimeCard>(hand.Hand).FindAll(c => c != null && c.Data.CardID == "S_BREAD" && c.Data is SpellData);
        foreach (var bread in breads)
        {
            if (game.IsGameOver || source.Zone != CardZone.Field || source.CurrentHealth <= 0) break;
            if (bread.Zone != CardZone.Hand) continue;
            hand.RemoveCardFromHand(bread);
            foreach (var effect in ((SpellData)bread.Data).SpellEffects)
                if (effect != null) effect.Resolve(bread, new List<RuntimeCard> { source });
            RuntimeCard.PublishTrigger(CardTriggerType.SpellCast, bread, bread);
            game.EffectManager.TriggerResonance(source.Owner);
            game.GetDeck(source.Owner)?.Discard(bread);
        }
        game.GetBattlefield(source.Owner)?.RefreshMinionView(source);
    }
}
