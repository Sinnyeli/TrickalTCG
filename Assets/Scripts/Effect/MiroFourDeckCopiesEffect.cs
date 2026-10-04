using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MiroFourDeckCopiesEffect", menuName = "Card Effects/Elemental/MiroFourDeckCopiesEffect")]
public class MiroFourDeckCopiesEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || targets == null || GameManager.Instance == null) return;
        var deck = GameManager.Instance.GetDeck(source.Owner);
        if (deck == null) return;
        foreach (var target in targets)
        {
            if (target == null || !(target.Data is MinionData) || target.Zone != CardZone.Field || target.CurrentHealth <= 0) continue;
            for (int i = 0; i < 4; i++) deck.AddCardToDeck(target.Data, false);
            deck.Shuffle();
            break;
        }
    }
}
