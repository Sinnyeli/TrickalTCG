using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Alice Prophecy Seed")]
public class AliceProphecySeedEffect : CardEffect
{
    [SerializeField] private List<SpellData> prophecies = new List<SpellData>();
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        var deck = source == null ? null : GameManager.Instance?.GetDeck(source.Owner);
        if (deck == null) return;
        foreach (var prophecy in prophecies) if (prophecy != null) deck.AddCardToDeck(prophecy, false);
        deck.Shuffle();
    }
}
