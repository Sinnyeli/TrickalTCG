using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CombinedEffect",
    menuName = "Card Effects/Combined"
)]
public class CombinedEffect : CardEffect
{
    [Header("Effects")]
    [SerializeField]
    private List<CardEffect> effects =
        new List<CardEffect>();


    public override void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets)
    {
        if (source == null)
            return;


        foreach (CardEffect effect in effects)
        {
            if (effect == null)
                continue;


            GameManager.Instance
                .EffectManager
                .ResolveCardEffect(
                    source,
                    effect
                );
        }
    }
}