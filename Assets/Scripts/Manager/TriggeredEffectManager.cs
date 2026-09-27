using System.Collections.Generic;
using UnityEngine;

public class TriggeredEffectManager : MonoBehaviour
{
    // =========================================================
    // SUBSCRIBE
    // =========================================================

    private void OnEnable()
    {
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
        RuntimeCard.OnStatsGained -=
            HandleStatsGained;

        BattlefieldManager.OnUnitSummoned -=
            HandleUnitSummoned;
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
            if (source == null)
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

            CardEffect passive =
                minionData.Passive;

            if (passive == null)
                continue;


            // =========================================
            // MUST BE A TRIGGERED EFFECT
            // =========================================

            if (!(passive
                is TriggeredEffect triggered))
            {
                continue;
            }


            // =========================================
            // CORRECT TRIGGER?
            // =========================================

            if (triggered.TriggerType !=
                triggerType)
            {
                continue;
            }


            // =========================================
            // RESOLVE
            // =========================================

            triggered.ResolveTrigger(
                source,
                triggerCard,
                modifier
            );
        }
    }
}