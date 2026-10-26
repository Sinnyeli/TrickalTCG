using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ShupangTurnEndDrawEffect",
    menuName = "Card Effects/Shupang/No Attack Turn End Draw"
)]
public class ShupangTurnEndDrawEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (
            source == null
            || source.IsSilenced
            || source.Zone != CardZone.Field
            || source.CurrentHealth <= 0
            || source.HasAttackedThisTurn
            || GameManager.Instance == null
        )
            return;
        if (GameManager.Instance.TurnManager.CurrentSide != source.Owner)
            return;
        GameManager.Instance.DrawCard(source.Owner);
    }
}
