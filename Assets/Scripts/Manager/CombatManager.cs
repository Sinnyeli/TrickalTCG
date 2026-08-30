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

        if (!turnManager.IsMyTurn(attacker.Owner))
            return false;

        if (!attacker.CanAttack)
        {
            Debug.Log(
                $"{attacker.Data.cardName} cannot attack yet."
            );

            return false;
        }

        selectedAttacker = attacker;

        Debug.Log(
            $"Selected attacker: {attacker.Data.cardName}"
        );

        return true;
    }

    public bool Attack(RuntimeCard defender)
    {
        if (selectedAttacker == null)
            return false;

        if (defender == null)
            return false;

        if (selectedAttacker.Owner == defender.Owner)
            return false;

        ResolveCombat(
            selectedAttacker,
            defender
        );

        selectedAttacker = null;

        return true;
    }

    private void ResolveCombat(
        RuntimeCard attacker,
        RuntimeCard defender)
    {
        int attackerDamage =
            attacker.GetAttack();

        int defenderDamage =
            defender.GetAttack();

        Debug.Log(
            $"{attacker.Data.cardName} attacks " +
            $"{defender.Data.cardName}"
        );

        defender.TakeDamage(attackerDamage);
        attacker.TakeDamage(defenderDamage);

        Debug.Log(
            $"{attacker.Data.cardName}: " +
            attacker.CurrentHealth
        );

        Debug.Log(
            $"{defender.Data.cardName}: " +
            defender.CurrentHealth
        );

        CheckDeath(attacker);
        CheckDeath(defender);
    }

    private void CheckDeath(RuntimeCard card)
    {
        if (card.CurrentHealth > 0)
            return;

        Debug.Log(
            $"{card.Data.cardName} has died."
        );

        GameManager.Instance.GetBattlefield(card.Owner).RemoveCard(card);
    }
}