using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PolangAuraEffect", menuName = "Card Effects/Polang/Other Sprite Aura")]
public class PolangAuraEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (
            source == null
            || source.IsSilenced
            || source.Zone != CardZone.Field
            || source.CurrentHealth <= 0
            || GameManager.Instance == null
        )
            return;
        var field = GameManager.Instance.GetBattlefield(source.Owner);
        if (field == null)
            return;
        foreach (var unit in new List<RuntimeCard>(field.Minions))
        {
            if (
                unit == null
                || unit == source
                || unit.CurrentHealth <= 0
                || !(unit.Data is MinionData data)
                || !data.HasRace(CardRace.Sprite)
            )
                continue;
            unit.AddModifier(new RuntimeModifier(1, 1, true, source, true));
        }
    }
}
