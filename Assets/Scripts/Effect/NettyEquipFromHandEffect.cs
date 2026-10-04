using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NettyEquipFromHandEffect", menuName = "Card Effects/Netty/Equip Random From Hand")]
public class NettyEquipFromHandEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || source.Zone != CardZone.Field || source.CurrentHealth <= 0 || GameManager.Instance == null) return;
        var hand = GameManager.Instance.GetHandManager(source.Owner);
        if (hand == null || source.EquippedArtifacts.Count >= source.GetMaxEquipment()) return;
        var candidates = new List<RuntimeCard>(hand.Hand).FindAll(c => c != null && c.Data is ArtifactData && c.Zone == CardZone.Hand);
        if (candidates.Count == 0) return;
        var artifact = candidates[Random.Range(0, candidates.Count)];
        hand.RemoveCardFromHand(artifact);
        if (!source.EquipArtifact(artifact)) { artifact.ChangeZone(CardZone.Hand); hand.AddCard(artifact); }
        GameManager.Instance.GetBattlefield(source.Owner)?.RefreshMinionView(source);
    }
}
