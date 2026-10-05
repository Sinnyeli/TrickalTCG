using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Witch Third Resonance")]
public class WitchThirdResonanceEffect : CardEffect
{
    [SerializeField] private bool copyLastSpell;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || source.IsSilenced || source.Zone != CardZone.Field || source.AdvanceAbilityCounter(this) % 3 != 0) return;
        var game = GameManager.Instance;
        if (!copyLastSpell) game.DrawCard(source.Owner);
        else
        {
            var spell = game.GetLastCastSpell(source.Owner);
            if (spell != null) game.GetHandManager(source.Owner)?.AddGeneratedCard(spell, source.Owner);
        }
    }
}
