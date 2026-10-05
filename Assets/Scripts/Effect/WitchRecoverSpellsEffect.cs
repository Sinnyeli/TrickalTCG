using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Witch Recover Spells")]
public class WitchRecoverSpellsEffect : CardEffect
{
    [SerializeField, Min(1)] private int amount = 2;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || GameManager.Instance == null) return;
        var game = GameManager.Instance; var deck = game.GetDeck(source.Owner); var hand = game.GetHandManager(source.Owner);
        if (deck == null || hand == null) return;
        var pool = deck.GetDiscardPileSnapshot().FindAll(card => card != null && card.Data is SpellData && card.Zone == CardZone.Graveyard);
        for (int i = 0; i < amount && pool.Count > 0; i++)
        {
            int index = Random.Range(0, pool.Count); var card = pool[index]; pool.RemoveAt(index);
            if (!deck.TryRecoverDiscardedCard(card)) continue;
            if (!hand.AddCard(card)) deck.Discard(card); // Overflow follows normal hand rules; returning is not drawing.
        }
    }
}
