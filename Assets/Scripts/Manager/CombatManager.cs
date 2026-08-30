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
    if (selectedAttacker == null)
    {
        Debug.Log("No attacker selected.");
        return false;
    }

    if (defender == null)
    {
        Debug.Log("No defender selected.");
        return false;
    }

    if (selectedAttacker.Owner == defender.Owner)
    {
        Debug.Log("Cannot attack your own minion.");
        return false;
    }

    RuntimeCard attacker = selectedAttacker;

    Debug.Log(
        $"{attacker.Data.cardName} attacks " +
        $"{defender.Data.cardName}"
    );

    int attackerDamage = attacker.GetAttack();
    int defenderDamage = defender.GetAttack();

    // Both minions deal damage.
    defender.TakeDamage(attackerDamage);
    attacker.TakeDamage(defenderDamage);

    GameManager.Instance.GetBattlefield(attacker.Owner).RefreshMinionView(attacker);

    GameManager.Instance.GetBattlefield(defender.Owner).RefreshMinionView(defender);
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



private void CheckDeath(RuntimeCard card)
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