using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
        private RuntimeCard selectedAttacker;

    public RuntimeCard SelectedAttacker =>
        selectedAttacker;

    public bool SelectAttacker(RuntimeCard attacker)
    {
        if (attacker == null)
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
public bool Attack(RuntimeCard defender)
{
    RuntimeCard attacker = selectedAttacker;

    if (selectedAttacker == null)
    {
        return false;
    }

    // Didn't select anything.
    if (defender == null)
    {
        Debug.Log("No defender selected.");
        return false;
    }

    // Cannot attack own minion.
    if (selectedAttacker.Owner == defender.Owner)
    {
        Debug.Log("Cannot attack your own minion.");
        return false;
    }

    Debug.Log(
        $"{attacker.Data.cardName} attacks " +
        $"{defender.Data.cardName}"
    );

    attacker.RemoveStealth();

    GameManager.Instance.EffectManager.ResolveOnAttack(attacker);

    int attackerDamage = attacker.GetAttack();
    int defenderDamage = defender.GetAttack();

    bool attackerHasFirstStrike =
        attacker.HasKeyword(CardKeyword.FirstStrike);

    if (attackerHasFirstStrike)
    {
        // Attacker strikes first.
        bool defenderTookDamage =
            defender.TakeDamage(attackerDamage);

        TriggerOnDamageTaken(
            defender,
            defenderTookDamage
        );

        // Shock from attacker.
        if (attacker.HasKeyword(CardKeyword.Shock))
        {
            defender.Kill();
        }

        // Defender only retaliates if still alive.
        if (defender.CurrentHealth > 0)
        {
            bool attackerTookDamage =
                attacker.TakeDamage(defenderDamage);

            TriggerOnDamageTaken(
                attacker,
                attackerTookDamage
            );

            // Shock from defender.
            if (defender.HasKeyword(CardKeyword.Shock))
            {
                attacker.Kill();
            }
        }
    }
    else
    {
        // Normal simultaneous combat.
        bool defenderTookDamage =
            defender.TakeDamage(attackerDamage);

        bool attackerTookDamage =
            attacker.TakeDamage(defenderDamage);

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
            defender.Kill();
        }

        // Shock from defender.
        if (defender.HasKeyword(CardKeyword.Shock))
        {
            attacker.Kill();
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
    if (selectedAttacker == null)
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
    attacker.RemoveStealth();



    GameManager.Instance.EffectManager.ResolveOnAttack(attacker);

    int attackerDamage = attacker.GetAttack();
    
    Debug.Log(
        $"{attacker.Data.cardName} attacks " +
        $"{defender.Side} player!"
    );

    defender.TakeDamage(attackerDamage);
    attacker.DisableAttack();

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
        if (card.HasKeyword(CardKeyword.Taunt))
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