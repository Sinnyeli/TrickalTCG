using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
        private RuntimeCard selectedAttacker;
    private bool allowForcedFriendlyFire;

    public RuntimeCard SelectedAttacker =>
        selectedAttacker;

    public bool SelectAttacker(RuntimeCard attacker)
    {
        if (attacker == null || attacker.Zone != CardZone.Field || attacker.CurrentHealth <= 0 || GameManager.Instance.IsGameOver)
            return false;

        TurnManager turnManager =
            GameManager.Instance.TurnManager;

        if (turnManager == null)
            return false;

        if (!turnManager.IsMyTurn(attacker.Owner))
        {
            Debug.Log(
                "This minion cannot attack right now."
            );

            return false;
        }

        if (!attacker.CanAttack)
        {
            Debug.Log(
                $"{attacker.Data.cardName} cannot attack yet."
            );

            return false;
        }

        selectedAttacker = attacker;

        Debug.Log(
            $"Selected attacker: " +
            $"{attacker.Data.cardName}"
        );

        return true;
    }
public bool ForceUnitAttack(RuntimeCard attacker, RuntimeCard defender, bool allowFriendlyFire = false)
{
    if (attacker == null || defender == null || attacker == defender || attacker.IsFrozen || attacker.Zone != CardZone.Field ||
        defender.Zone != CardZone.Field || attacker.CurrentHealth <= 0 || defender.CurrentHealth <= 0 ||
        (!allowFriendlyFire && attacker.Owner == defender.Owner)) return false;
    var oldSelection = selectedAttacker;
    bool wasReady = attacker.CanAttack, oldFriendlyFire = allowForcedFriendlyFire;
    bool success = false;
    attacker.EnableAttack(); selectedAttacker = attacker; allowForcedFriendlyFire = allowFriendlyFire;
    try { success = Attack(defender); return success; }
    finally
    {
        if (!success && !wasReady) attacker.DisableAttack();
        allowForcedFriendlyFire = oldFriendlyFire;
        selectedAttacker = oldSelection != null && oldSelection.Zone == CardZone.Field && oldSelection.CanAttack ? oldSelection : null;
    }
}

public bool Attack(RuntimeCard defender)
{
    RuntimeCard attacker = selectedAttacker;

    if (selectedAttacker == null || !selectedAttacker.CanAttack || selectedAttacker.Zone != CardZone.Field || selectedAttacker.CurrentHealth <= 0)
    {
        return false;
    }

    if (GameManager.Instance.IsGameOver ||
        defender == null || defender.Zone != CardZone.Field || defender.CurrentHealth <= 0 || defender.IsStealthed)
        return false;

    // Didn't select anything.
    if (defender == null)
    {
        Debug.Log("No defender selected.");
        return false;
    }

    // Cannot attack own minion.
    if (!allowForcedFriendlyFire && selectedAttacker.Owner == defender.Owner)
    {
        Debug.Log("Cannot attack your own minion.");
        return false;
    }

    // =========================================
// TAUNT CHECK
// =========================================

    PlayerSide defendingSide =
        defender.Owner;

    if (attacker.Owner != defender.Owner && HasTaunt(defendingSide) &&
        !defender.HasKeyword(CardKeyword.Taunt))
    {
        Debug.Log(
            "Cannot attack another unit while " +
            "a Taunt unit is on the battlefield."
        );

        return false;
    }


    Debug.Log(
        $"{attacker.Data.cardName} attacks " +
        $"{defender.Data.cardName}"
    );

    MinionViewBase defenderView = CombatPresentation.FindView(defender);
    if (defenderView != null) CombatPresentation.Attack(attacker, defenderView.transform);
    attacker.MarkAttacked();
    attacker.RemoveStealth();

    defender.ResolveBeforeDefending();
    if (defender.Zone != CardZone.Field || defender.CurrentHealth <= 0)
    {
        attacker.DisableAttack(); selectedAttacker = null; return true;
    }
    RuntimeCard.PublishTrigger(CardTriggerType.AttackStarted, attacker, defender);
    GameManager.Instance.EffectManager.ResolveOnAttack(attacker);
    attacker.ResolveMayoAttack(defender);

    int attackerDamage = attacker.GetCombatDamage();
    int defenderDamage = defender.GetCombatDamage();

    bool attackerHasFirstStrike =
        attacker.HasKeyword(CardKeyword.FirstStrike);

    if (attackerHasFirstStrike)
    {
        // Attacker strikes first.
        bool defenderTookDamage =
            defender.TakeDamage(attackerDamage, attacker);
            

        TriggerOnDamageTaken(
            defender,
            defenderTookDamage
        );

        // Shock from attacker.
        if (attacker.HasKeyword(CardKeyword.Shock))
        {
            defender.Kill(attacker);
        }

        // Defender only retaliates if still alive.
        if (defender.CurrentHealth > 0)
        {
            bool attackerTookDamage =
                attacker.TakeDamage(defenderDamage, defender);

            TriggerOnDamageTaken(
                attacker,
                attackerTookDamage
            );

            // Shock from defender.
            if (defender.HasKeyword(CardKeyword.Shock))
            {
                attacker.Kill(defender);
            }
        }
    }
    else
    {
        // Normal simultaneous combat.
        bool defenderTookDamage =
            defender.TakeDamage(attackerDamage, attacker);

        bool attackerTookDamage =
            attacker.TakeDamage(defenderDamage, defender);

        TriggerOnDamageTaken(
            defender,
            defenderTookDamage
        );

        TriggerOnDamageTaken(
            attacker,
            attackerTookDamage
        );

        // Shock from attacker.
        if (attacker.HasKeyword(CardKeyword.Shock))
        {
            defender.Kill(attacker);
        }

        // Shock from defender.
        if (defender.HasKeyword(CardKeyword.Shock))
        {
            attacker.Kill(defender);
        }
    }

    GameManager.Instance
        .GetBattlefield(attacker.Owner)
        .RefreshMinionView(attacker);

    GameManager.Instance
        .GetBattlefield(defender.Owner)
        .RefreshMinionView(defender);

    attacker.DisableAttack();

    CheckDeath(attacker);
    CheckDeath(defender);

    selectedAttacker = null;

    return true;
}


public bool Attack(PlayerView defender)
{
    if (selectedAttacker == null || !selectedAttacker.CanAttack || selectedAttacker.Zone != CardZone.Field || selectedAttacker.CurrentHealth <= 0)
    {
        Debug.Log("No attacker selected.");
        return false;
    }

    if (defender == null)
    {
        Debug.Log("No PlayerView selected.");
        return false;
    }

    if (selectedAttacker.Owner == defender.Side)
    {
        Debug.Log("Cannot attack your own player.");
        return false;
    }

    RuntimeCard attacker = selectedAttacker;
    if (GameManager.Instance.IsGameOver) return false;

         // Rush restriction.
    if (attacker.CannotAttackHero)
    {
        Debug.Log(
            $"{attacker.Data.cardName} cannot attack the Hero this turn."
        );

        return false;
    }

    // Taunt restriction. Unless opponent has bypass.
    if (HasTaunt(defender.Side) &&
        !attacker.HasKeyword(CardKeyword.Bypass))
    {
        Debug.Log(
            "Cannot attack the Hero while a Taunt minion remains."
        );

        return false;
    }
    CombatPresentation.Attack(attacker, defender.transform);
    attacker.MarkAttacked();
    attacker.RemoveStealth();



    RuntimeCard.PublishTrigger(CardTriggerType.AttackStarted, attacker, null);
    GameManager.Instance.EffectManager.ResolveOnAttack(attacker);

    int attackerDamage = attacker.GetCombatDamage();
    
    Debug.Log(
        $"{attacker.Data.cardName} attacks " +
        $"{defender.Side} player!"
    );

    defender.TakeDamage(attackerDamage);
    attacker.DisableAttack();
    Debug.Log(
    $"HERO ATTACK TARGET | " +
    $"Object={defender.gameObject.name} | " +
    $"InstanceID={defender.GetInstanceID()} | " +
    $"Side={defender.Side} | " +
    $"HP={defender.CurrentHealth}/{defender.MaxHealth} | " +
    $"Damage={attackerDamage}"
    );

    ClearSelection();

    return true;
}

///////////
///  Check if opponent side has taunt while attacking. This is a boolean. True/False thing.
/// ///////

private bool HasTaunt(PlayerSide defendingSide)
{
    BattlefieldManager battlefield =
        GameManager.Instance.GetBattlefield(defendingSide);

    foreach (RuntimeCard card in battlefield.Minions)
    {
        if (card.HasKeyword(CardKeyword.Taunt) && !card.IsStealthed)
            return true;
    }

    return false;
}

private void TriggerOnDamageTaken(
    RuntimeCard card,
    bool tookDamage)
{
    if (!tookDamage)
        return;

    if (card == null)
        return;

    if (card.CurrentHealth <= 0)
        return;

    GameManager.Instance
        .EffectManager
        .ResolveOnDamageTaken(card);
}


public void CheckDeath(RuntimeCard card)
{
    if (card == null)
        return;

    if (card.CurrentHealth > 0)
        return;

    Debug.Log(
        $"{card.Data.cardName} has died."
    );

    BattlefieldManager battlefield =
        GameManager.Instance.GetBattlefield(card.Owner);

    if (battlefield == null)
        return;

    battlefield.RemoveCard(card);
}




    public void ClearSelection()
    {
        selectedAttacker = null;
    }
}