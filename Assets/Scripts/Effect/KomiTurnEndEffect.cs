using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "KomiTurnEndEffect", menuName = "Card Effects/Komi/No Attack Turn End Buff")]
public class KomiTurnEndEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || source.IsSilenced || source.Zone != CardZone.Field ||
            source.CurrentHealth <= 0 || source.HasAttackedThisTurn || GameManager.Instance == null) return;
        if (GameManager.Instance.TurnManager.CurrentSide != source.Owner) return;
        source.AddModifier(new RuntimeModifier(1, 1, true, source));
        GameManager.Instance.GetBattlefield(source.Owner)?.RefreshMinionView(source);
    }
}
