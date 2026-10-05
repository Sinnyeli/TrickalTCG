using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
        private RuntimeCard selectedAttacker;
    private bool allowForcedFriendlyFire;
    private int forcedAttackDepth;

    private bool Fail(string reason, RuntimeCard attacker = null)
    {
        string message = CombatFailureUI.Message(reason);
        Debug.Log(message);
        // Automatic attacks and opponent AI keep diagnostic logs without player notices.
        if (forcedAttackDepth == 0 && (attacker == null || attacker.Owner == PlayerSide.Player))
            CombatFailureUI.Show(reason);
        return false;
    }

    private bool FailNotReady(RuntimeCard attacker)
    {
        string reason = DescribedSpellState.AttacksBlocked ? "BLOCKED" :
            attacker.IsFrozen ? "FROZEN" : attacker.HasAttackedThisTurn ? "ALREADY_ATTACKED" : "NOT_READY";
        return Fail(reason, attacker);
    }


    public RuntimeCard SelectedAttacker =>
        selectedAttacker;

    public bool SelectAttacker(RuntimeCard attacker)
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return false;
        if (attacker == null || attacker.Zone != CardZone.Field || attacker.CurrentHealth <= 0)
            return Fail("INVALID_UNIT", attacker);
        var turnManager = GameManager.Instance.TurnManager;
        if (turnManager == null) return false;
        if (!turnManager.IsMyTurn(attacker.Owner)) return Fail("NOT_TURN", attacker);
        if (!attacker.CanAttack) return FailNotReady(attacker);

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
    forcedAttackDepth++;
    try { success = Attack(defender); return success; }
    finally
    {
        forcedAttackDepth--;
        if (!success && !wasReady) attacker.DisableAttack();
        allowForcedFriendlyFire = oldFriendlyFire;
        selectedAttacker = oldSelection != null && oldSelection.Zone == CardZone.Field && oldSelection.CanAttack ? oldSelection : null;
    }
}

public bool Attack(RuntimeCard defender)
{
    RuntimeCard attacker = selectedAttacker;

    if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return false;
    if (attacker == null) return Fail("NO_ATTACKER");
    if (attacker.Zone != CardZone.Field || attacker.CurrentHealth <= 0) return Fail("INVALID_UNIT", attacker);
    if (forcedAttackDepth == 0 && !GameManager.Instance.TurnManager.IsMyTurn(attacker.Owner)) return Fail("NOT_TURN", attacker);
    if (!attacker.CanAttack) return FailNotReady(attacker);
    if (defender == null) return Fail("NO_TARGET", attacker);
    if (defender.Zone != CardZone.Field || defender.CurrentHealth <= 0) return Fail("INVALID_UNIT", attacker);
    if (defender.IsStealthed) return Fail("STEALTH", attacker);
    if (!allowForcedFriendlyFire && attacker.Owner == defender.Owner) return Fail("OWN_UNIT", attacker);

    // =========================================
// TAUNT CHECK
// =========================================

    PlayerSide defendingSide =
        defender.Owner;

    if (attacker.Owner != defender.Owner && HasTaunt(defendingSide) &&
        !defender.HasKeyword(CardKeyword.Taunt))
    {
        return Fail("TAUNT_UNIT", attacker);
    }


    Debug.Log(
        $"{attacker.Data.cardName} attacks " +
        $"{defender.Data.cardName}"
    );

    MinionViewBase defenderView = CombatPresentation.FindView(defender);
    if (defenderView != null) CombatPresentation.Attack(attacker, defenderView.transform);
    DescribedSpellState.BeforeAttack(attacker, defender);
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
    DescribedSpellState.AfterAttack(attacker, defender);

    CheckDeath(attacker);
    CheckDeath(defender);

    selectedAttacker = null;

    return true;
}


public bool Attack(PlayerView defender)
{
    if (GameManager.Instance == null || GameManager.Instance.IsGameOver) return false;
    RuntimeCard attacker = selectedAttacker;
    if (attacker == null) return Fail("NO_ATTACKER");
    if (attacker.Zone != CardZone.Field || attacker.CurrentHealth <= 0) return Fail("INVALID_UNIT", attacker);
    if (!GameManager.Instance.TurnManager.IsMyTurn(attacker.Owner)) return Fail("NOT_TURN", attacker);
    if (!attacker.CanAttack) return FailNotReady(attacker);
    if (defender == null) return Fail("NO_TARGET", attacker);
    if (attacker.Owner == defender.Side) return Fail("OWN_PLAYER", attacker);
    if (attacker.CannotAttackHero) return Fail("RUSH", attacker);
    if (HasTaunt(defender.Side) && !attacker.HasKeyword(CardKeyword.Bypass)) return Fail("TAUNT_HERO", attacker);
    CombatPresentation.Attack(attacker, defender.transform);
    DescribedSpellState.BeforeAttack(attacker, null);
    GameManager.Instance.GetBattlefield(attacker.Owner).RefreshMinionView(attacker);
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