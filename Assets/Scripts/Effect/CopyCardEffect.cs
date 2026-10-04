using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CopyCardEffect", menuName = "Card Effects/CopyCard")]
public class CopyCardEffect : CardEffect
{
    public enum Destination { Hand, Field }
    [SerializeField] private Destination destination = Destination.Field;
    [SerializeField] private bool overrideStats = true;
    [Min(0), SerializeField] private int attack = 1;
    [Min(1), SerializeField] private int health = 1;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || targets == null || GameManager.Instance == null) return;
        var game = GameManager.Instance;
        foreach (var target in new List<RuntimeCard>(targets))
        {
            if (target == null || !(target.Data is MinionData)) continue;
            var copy = new RuntimeCard(target.Data); // Fresh abilities/keywords, not attached equipment or buffs.
            copy.SetOwner(source.Owner);
            if (overrideStats) copy.SetBaseStatOverride(Mathf.Max(0, attack), Mathf.Max(1, health), source, false);
            copy.ChangeZone(CardZone.Hand);
            if (destination == Destination.Hand)
            {
                var hand = game.GetHandManager(source.Owner);
                if (hand == null || !hand.AddCard(copy)) game.GetDeck(source.Owner)?.Discard(copy);
            }
            else
            {
                var field = game.GetBattlefield(source.Owner);
                if (field == null || !field.HasAvailableMinionSlot) continue;
                field.PlayCard(copy);
            }
        }
    }
}
