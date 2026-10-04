using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InkleCopyHandSpellsEffect", menuName = "Card Effects/Elemental/InkleCopyHandSpellsEffect")]
public class InkleCopyHandSpellsEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || GameManager.Instance == null) return;
        var hand = GameManager.Instance.GetHandManager(source.Owner);
        if (hand == null) return;
        var spells = new List<RuntimeCard>(hand.Hand).FindAll(c => c != null && c.Data is SpellData);
        foreach (var spell in spells) hand.AddGeneratedCard(spell.Data, source.Owner);
    }
}
