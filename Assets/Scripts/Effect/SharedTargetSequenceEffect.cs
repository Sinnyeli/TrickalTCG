using System.Collections.Generic;
using UnityEngine;

/// <summary>Resolve ordered unit operations against one selection. Child target settings are ignored.</summary>
[CreateAssetMenu(menuName = "Card Effects/Shared Target Sequence", fileName = "SharedTargetSequenceEffect")]
public class SharedTargetSequenceEffect : CardEffect
{
    [SerializeField] private List<CardEffect> effects = new List<CardEffect>();

    private bool resolving;

    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || targets == null || resolving) return;
        resolving = true;
        try
        {
        foreach (CardEffect effect in effects)
        {
            if (effect == null || effect == this) continue;
            // Do not apply later operations to units removed by an earlier operation.
            var remaining = targets.FindAll(target => target != null && target.Zone == CardZone.Field);
            if (remaining.Count == 0) break;
            effect.Resolve(source, remaining);
        }
        }
        finally { resolving = false; }
    }
}
