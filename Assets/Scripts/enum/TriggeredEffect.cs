using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "TriggeredEffect",
    menuName = "Card Effects/Triggered Effect"
)]
public class TriggeredEffect : CardEffect
{
    [Header("Trigger")]
    [SerializeField]
    private CardTriggerType triggerType;

    [Header("Trigger Conditions")]
    [SerializeField]
    private List<TriggerCondition> conditions =
        new List<TriggerCondition>();

    [Header("Effects")]
    [SerializeField]
    private List<CardEffect> effects =
        new List<CardEffect>();


    public CardTriggerType TriggerType =>
        triggerType;


    // =========================================================
    // NORMAL RESOLVE
    // =========================================================

    public override void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets)
    {
        // Triggered effects don't resolve immediately.
        // TriggeredEffectManager activates them when
        // their trigger occurs.
    }


    // =========================================================
    // TRIGGER
    // =========================================================

    public void ResolveTrigger(
        RuntimeCard source,
        RuntimeCard triggerCard,
        RuntimeModifier modifier = null)
    {
        if (source == null)
            return;


        // =========================================
        // CONDITIONS
        // =========================================

        foreach (TriggerCondition condition
                 in conditions)
        {
            if (condition == null)
                continue;

            if (!condition.IsMet(
                    source,
                    triggerCard,
                    modifier))
            {
                return;
            }
        }


        // =========================================
        // EFFECTS
        // =========================================

        List<RuntimeCard> targets =
            new List<RuntimeCard>();

        if (triggerCard != null)
        {
            targets.Add(triggerCard);
        }


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