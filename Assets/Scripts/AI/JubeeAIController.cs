using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JubeeAIController : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField]
    private HandManager handManager;

    [SerializeField]
    private BattlefieldManager battlefieldManager;

    [SerializeField]
    private BattlefieldManager enemyBattlefieldManager;

    [SerializeField]
    private CombatManager combatManager;

    [SerializeField]
    private TurnManager turnManager;

    [Header("Target")]
    [SerializeField]
    private PlayerView enemyPlayerView;


    [Header("Timing")]
    [SerializeField]
    private float actionDelay = 0.5f;


    private bool isTakingTurn;

    private void Start()
    {
        if (turnManager == null)
        {
            Debug.LogError(
                "JubeeAIController has no TurnManager assigned."
            );

            return;
        }

        turnManager.OnTurnStarted +=
            HandleTurnStarted;

        Debug.Log(
            "Jubee AI subscribed to TurnManager."
        );
    }

    // =========================================================
    // TAKE TURN
    // =========================================================

    public void TakeTurn()
    {
        if (isTakingTurn)
            return;

        if (turnManager == null)
            return;

        if (!turnManager.IsMyTurn(
                PlayerSide.Opponent))
        {
            return;
        }

        StartCoroutine(
            TakeTurnRoutine()
        );
    }


    // =========================================================
    // TURN ROUTINE
    // =========================================================

    private IEnumerator TakeTurnRoutine()
    {
        isTakingTurn = true;

        Debug.Log(
            "Jubee AI begins turn."
        );

        yield return new WaitForSeconds(
            actionDelay
        );


        // =========================================
        // PLAY CARDS
        // =========================================

        yield return PlayCards();


        // =========================================
        // ATTACK
        // =========================================

        yield return AttackWithMinions();


        // =========================================
        // END TURN
        // =========================================

        yield return new WaitForSeconds(
            actionDelay
        );

        Debug.Log(
            "Jubee AI ends turn."
        );

        isTakingTurn = false;

        if (turnManager.IsMyTurn(
                PlayerSide.Opponent))
        {
            turnManager.EndTurn();
        }
    }


    // =========================================================
    // PLAY CARDS
    // =========================================================

    private IEnumerator PlayCards()
    {
        bool playedCard;

        do
        {
            playedCard = false;

            // Copy because playing a card modifies
            // HandManager.Hand.
            List<RuntimeCard> cards =
                new List<RuntimeCard>(
                    handManager.Hand
                );


            foreach (RuntimeCard card in cards)
            {
                if (card == null)
                    continue;

                if (!turnManager.IsMyTurn(
                        PlayerSide.Opponent))
                {
                    yield break;
                }


                int cost =
                    card.GetManaCost();


                if (!turnManager.CanSpendMana(
                        PlayerSide.Opponent,
                        cost))
                {
                    continue;
                }


                // -----------------------------------------
                // COMMANDER
                // -----------------------------------------

                if (card.IsCommander)
                {
                    bool success =
                        handManager
                            .PlayCommanderFromHand(
                                card
                            );

                    if (success)
                    {
                        playedCard = true;

                        Debug.Log(
                            $"Jubee AI played Commander: " +
                            $"{card.Data.cardName}"
                        );

                        yield return
                            new WaitForSeconds(
                                actionDelay
                            );

                        break;
                    }

                    continue;
                }


                // -----------------------------------------
                // NORMAL CARD
                // -----------------------------------------

                bool played =
                    handManager
                        .PlayCardFromHand(
                            card
                        );


                if (played)
                {
                    playedCard = true;

                    Debug.Log(
                        $"Jubee AI played: " +
                        $"{card.Data.cardName}"
                    );

                    yield return
                        new WaitForSeconds(
                            actionDelay
                        );

                    break;
                }
            }

        }
        while (playedCard);
    }


    // =========================================================
    // ATTACK
    // =========================================================

    private IEnumerator AttackWithMinions()
    {
        List<RuntimeCard> attackers =
            new List<RuntimeCard>(
                battlefieldManager.Minions
            );


        foreach (RuntimeCard attacker
                 in attackers)
        {
            if (attacker == null)
                continue;

            if (!turnManager.IsMyTurn(
                    PlayerSide.Opponent))
            {
                yield break;
            }


            if (!attacker.CanAttack)
                continue;


            bool selected =
                combatManager.SelectAttacker(
                    attacker
                );


            if (!selected)
                continue;


            yield return new WaitForSeconds(
                actionDelay
            );


            bool attacked =
                TryAttackEnemyMinion();


            // If no valid minion attack happened,
            // try the player directly.
            if (!attacked)
            {
                attacked =
                    combatManager.Attack(
                        enemyPlayerView
                    );
            }


            // Important:
            // Attack(hero) can fail due to Taunt,
            // Rush restriction, etc.
            if (!attacked)
            {
                combatManager.ClearSelection();
            }


            yield return new WaitForSeconds(
                actionDelay
            );
        }
    }


    // =========================================================
    // CHOOSE ENEMY
    // =========================================================

    private bool TryAttackEnemyMinion()
    {
        if (enemyBattlefieldManager == null)
            return false;


        List<RuntimeCard> targets =
            new List<RuntimeCard>();


        foreach (RuntimeCard target
                 in enemyBattlefieldManager.Minions)
        {
            if (target == null)
                continue;

            if (target.IsStealthed)
                continue;

            targets.Add(
                target
            );
        }


        if (targets.Count == 0)
            return false;


        RuntimeCard targetCard =
            targets[
                Random.Range(
                    0,
                    targets.Count
                )
            ];


        return combatManager.Attack(
            targetCard
        );
    }

private void OnDestroy()
{
    if (turnManager != null)
    {
        turnManager.OnTurnStarted -=
            HandleTurnStarted;
    }
}
private void HandleTurnStarted(
    PlayerSide side)
{
    if (side != PlayerSide.Opponent)
        return;

    TakeTurn();
}
}