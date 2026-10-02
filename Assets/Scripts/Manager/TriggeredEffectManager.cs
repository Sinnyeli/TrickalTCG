using System.Collections.Generic;
using UnityEngine;

public class TriggeredEffectManager : MonoBehaviour
{
    // =========================================================
    // SUBSCRIBE
    // =========================================================

    private void OnEnable()
    {
        RuntimeCard.OnGameplayTrigger += HandleGameplayTrigger;
        RuntimeCard.OnStatsGained +=
            HandleStatsGained;

        BattlefieldManager.OnUnitSummoned +=
            HandleUnitSummoned;
    }


    // =========================================================
    // UNSUBSCRIBE
    // =========================================================

    private void OnDisable()
    {
        RuntimeCard.OnGameplayTrigger -= HandleGameplayTrigger;
        RuntimeCard.OnStatsGained -=
            HandleStatsGained;

        BattlefieldManager.OnUnitSummoned -=
            HandleUnitSummoned;
    }


    private int triggerDepth;
    private int triggerBudget;

    private void HandleGameplayTrigger(CardTriggerType type, RuntimeCard actor, RuntimeCard subject, RuntimeModifier snapshot)
    {
        if (triggerDepth == 0) triggerBudget = 256;
        if (triggerDepth >= 32 || --triggerBudget < 0)
        {
            Debug.LogError("Trigger chain exceeded its safety limit; remaining event skipped.");
            return;
        }
        triggerDepth++;
        try
        {
            DispatchSide(PlayerSide.Player, type, actor, subject, snapshot);
            DispatchSide(PlayerSide.Opponent, type, actor, subject, snapshot);
        }
        finally { triggerDepth--; }
    }

    private void DispatchSide(PlayerSide side, CardTriggerType type, RuntimeCard actor, RuntimeCard subject, RuntimeModifier snapshot)
    {
        var field = GameManager.Instance?.GetBattlefield(side);
        if (field == null) return;
        foreach (var source in new List<RuntimeCard>(field.Minions))
        {
            if (source == null || source.Zone != CardZone.Field || source.CurrentHealth <= 0 || source.IsSilenced || !(source.Data is MinionData data)) continue;
            foreach (var passive in new List<CardEffect>(data.Passives))
                if (passive is TriggeredEffect effect && effect.TriggerType == type)
                    effect.ResolveGameplayTrigger(source, actor, subject, snapshot);
        }
    }

    // =========================================================
    // UNIT SUMMONED
    // =========================================================

    private void HandleUnitSummoned(
        RuntimeCard summonedCard)
    {
        ResolveBattlefieldTriggers(
            CardTriggerType.UnitSummoned,
            summonedCard,
            null
        );
    }


    // =========================================================
    // STATS GAINED
    // =========================================================

    private void HandleStatsGained(
        RuntimeCard card,
        RuntimeModifier modifier)
    {
        ResolveBattlefieldTriggers(
            CardTriggerType.StatsGained,
            card,
            modifier
        );
    }


    // =========================================================
    // RESOLVE BOTH SIDES
    // =========================================================

    private void ResolveBattlefieldTriggers(
        CardTriggerType triggerType,
        RuntimeCard triggerCard,
        RuntimeModifier modifier)
    {
        ResolveSide(
            PlayerSide.Player,
            triggerType,
            triggerCard,
            modifier
        );

        ResolveSide(
            PlayerSide.Opponent,
            triggerType,
            triggerCard,
            modifier
        );
    }


    // =========================================================
    // RESOLVE SIDE
    // =========================================================

    private void ResolveSide(
        PlayerSide side,
        CardTriggerType triggerType,
        RuntimeCard triggerCard,
        RuntimeModifier modifier)
    {
        if (GameManager.Instance == null)
            return;


        BattlefieldManager battlefield =
            GameManager.Instance.GetBattlefield(
                side
            );

        if (battlefield == null)
            return;


        // Copy because effects could modify the field
        // while triggers are resolving.
        List<RuntimeCard> cards =
            new List<RuntimeCard>(
                battlefield.Minions
            );


        foreach (RuntimeCard source in cards)
        {
            if (source == null || source.Zone != CardZone.Field || source.CurrentHealth <= 0)
                continue;

            if (source.IsSilenced)
                continue;


            // =========================================
            // MUST BE A MINION
            // =========================================

            if (!(source.Data
                is MinionData minionData))
            {
                continue;
            }


            // =========================================
            // GET PASSIVE
            // =========================================

            foreach (CardEffect passive in minionData.Passives)
            {
                if (!(passive is TriggeredEffect triggered) ||
                    triggered.TriggerType != triggerType)
                    continue;

                triggered.ResolveTrigger(source, triggerCard, modifier);
            }
        }
    }
}
