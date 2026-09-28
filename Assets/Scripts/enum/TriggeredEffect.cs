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
        // Triggered effects do not resolve immediately.
        // TriggeredEffectManager activates them when
        // the appropriate gameplay event occurs.
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


        // =====================================================
        // CHECK CONDITIONS
        // =====================================================

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


        // =====================================================
        // RESOLVE CHILD EFFECTS
        // =====================================================

        foreach (CardEffect effect in effects)
        {
            if (effect == null)
                continue;


            // =================================================
            // TRIGGER CARD TARGET
            //
            // Used for effects such as Epica:
            //
            // "After you summon a unit,
            // give IT +1/+1."
            // =================================================

            if (effect.TargetType ==
                EffectTargetType.TriggerCard)
            {
                if (triggerCard == null)
                    continue;

                // Make sure the trigger card also passes
                // the effect's normal target filter.
                if (!effect.MatchesTargetFilter(
                        triggerCard))
                {
                    continue;
                }


                List<RuntimeCard> triggerTargets =
                    new List<RuntimeCard>
                    {
                        triggerCard
                    };


                effect.Resolve(
                    source,
                    triggerTargets
                );

                continue;
            }


            // =================================================
            // NORMAL TARGETING
            //
            // Self, FriendlyUnit, EnemyUnit, etc.
            // continue through the normal EffectManager.
            //
            // Used for effects such as Diana:
            //
            // StatsGained trigger
            // -> condition checks the Beastfolk
            // -> BuffEffect targets Diana herself.
            // =================================================

            if (GameManager.Instance == null ||
                GameManager.Instance.EffectManager == null)
            {
                Debug.LogError(
                    "TriggeredEffect could not find " +
                    "EffectManager."
                );

                continue;
            }


            GameManager.Instance
                .EffectManager
                .ResolveCardEffect(
                    source,
                    effect
                );
        }
    }
}