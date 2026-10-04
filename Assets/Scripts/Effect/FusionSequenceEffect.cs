using System.Collections.Generic;
using UnityEngine;

// Each child retains its own targeting. Repeated slots stack rather than overwrite.
public class FusionSequenceEffect : CardEffect
{
    private readonly List<CardEffect> children = new List<CardEffect>();
    public static CardEffect Create(List<CardEffect> effects)
    {
        if (effects.Count == 0) return null;
        var sequence = CreateInstance<FusionSequenceEffect>();
        foreach (var effect in effects)
        {
            if (effect is FusionSequenceEffect nested)
                foreach (var child in nested.children) sequence.children.Add(Instantiate(child));
            else sequence.children.Add(Instantiate(effect));
        }
        return sequence;
    }
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        GameManager.Instance.EffectManager.ResolveFusionSequence(source, children);
    }
}
