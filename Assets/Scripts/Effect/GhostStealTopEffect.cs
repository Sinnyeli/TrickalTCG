using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Ghost Steal Top")]
public class GhostStealTopEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || GameManager.Instance == null) return;
        var enemy = source.Owner == PlayerSide.Player ? PlayerSide.Opponent : PlayerSide.Player;
        var card = GameManager.Instance.GetDeck(enemy)?.DrawCard();
        if (card != null) GameManager.Instance.HandleDrawnCard(card, source.Owner);
    }
}
