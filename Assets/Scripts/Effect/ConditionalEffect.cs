using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ConditionalEffect",
    menuName = "Card Effects/Conditional"
)]
public class ConditionalEffect : CardEffect
{
    [Header("Condition")]
    [SerializeField]
    private CardEffectCondition condition;


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


        // =========================================
        // CHECK CONDITION
        // =========================================

        if (condition == null)
        {
            Debug.LogWarning(
                "ConditionalEffect has no condition."
            );

            return;
        }


        if (!condition.IsMet(source))
        {
            Debug.Log(
                $"{source.Data.cardName}: " +
                $"condition not met."
            );

            return;
        }


        // =========================================
        // RESOLVE CHILD EFFECTS
        // =========================================

        foreach (CardEffect effect in effects)
        {
            if (effect == null)
                continue;

            effect.Resolve(
                source,
                targets
            );
        }
    }
}