using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MuteDestroyTopDeckEffect", menuName = "Card Effects/Elemental/MuteDestroyTopDeckEffect")]
public class MuteDestroyTopDeckEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || GameManager.Instance == null) return;
        var side = source.Owner == PlayerSide.Player ? PlayerSide.Opponent : PlayerSide.Player;
        GameManager.Instance.GetDeck(side)?.DestroyTopCard();
    }
}
