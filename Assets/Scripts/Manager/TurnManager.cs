using System;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("Turn Settings")]
    [SerializeField] private int maxMana = 10;

    [SerializeField, Min(1f)] private float turnDurationSeconds = 75f;
    private float remainingTurnSeconds;
    private bool timerRunning;
    private bool endingTurn;
    public float RemainingTurnSeconds => remainingTurnSeconds;
    public bool CanPlayerEndTurn => timerRunning && !endingTurn &&
        currentSide == PlayerSide.Player && GameManager.Instance != null &&
        !GameManager.Instance.IsGameOver;

    private void Update()
    {
        if (!timerRunning || GameManager.Instance == null) return;
        if (GameManager.Instance.IsGameOver)
        {
            timerRunning = false;
            return;
        }
        remainingTurnSeconds = Mathf.Max(0f, remainingTurnSeconds - Time.deltaTime);
        if (remainingTurnSeconds <= 0f) EndTurn();
    }

    // Only the local player's UI may call this entry point.
    public void EndPlayerTurn()
    {
        if (CanPlayerEndTurn) EndTurn();
    }

    private PlayerSide currentSide;
    private int turnNumber;

    private int playerMana;
    private int opponentMana;

    private int playerMaxMana;
    private int opponentMaxMana;

    public PlayerSide CurrentSide => currentSide;
    public int TurnNumber => turnNumber;

     public int PlayerMana => playerMana;
    public int OpponentMana => opponentMana;

    public int PlayerMaxMana => playerMaxMana;
    public int OpponentMaxMana => opponentMaxMana;
    [SerializeField]
   
    public event Action<PlayerSide> OnTurnStarted;

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
    timerRunning = false;
    endingTurn = false;
    turnNumber = 1;

    playerMaxMana = 0;
    opponentMaxMana = 0;

    playerMana = 0;
    opponentMana = 0;
    currentSide = PlayerSide.Player;
    StartTurn();

/*        Debug.Log(
            $"Turn {turnNumber} started. " +
            $"Current Side: {currentSide}. " +
            $"Mana: {playerMana}");*/
    }



public void StartTurn()
{
     if (GameManager.Instance.IsGameOver)
        return;
    if (currentSide == PlayerSide.Player)
    {
        playerMaxMana = Mathf.Min(
            playerMaxMana + 1,
            maxMana
        );

        playerMana = playerMaxMana;
         GameManager.Instance.PlayerBattlefieldManager.RefreshAttackers(PlayerSide.Player);
    }
    else
    {
        opponentMaxMana = Mathf.Min(
            opponentMaxMana + 1,
            maxMana
        );

        opponentMana = opponentMaxMana;
        GameManager.Instance.OpponentBattlefieldManager.RefreshAttackers(PlayerSide.Opponent);
    }

   GameManager.Instance
        .EffectManager
        .TriggerTurnStart(currentSide);
    
    if (GameManager.Instance.IsGameOver)
        return;
// =========================================
// AI TURN
// =========================================
    GameManager.Instance.DrawCard(currentSide);
    if (GameManager.Instance.IsGameOver) return;
    remainingTurnSeconds = Mathf.Max(1f, turnDurationSeconds);
    timerRunning = true;
    OnTurnStarted?.Invoke(currentSide);
}
    public void EndTurn()
    {
        if (!timerRunning || endingTurn || GameManager.Instance.IsGameOver)
            return;
        timerRunning = false;
        endingTurn = true;
        try
        {
            if (EffectTargetManager.Instance != null)
                EffectTargetManager.Instance.CancelTargetSelection();
            if (GameManager.Instance.CombatManager != null)
                GameManager.Instance.CombatManager.ClearSelection();
            Debug.Log(
                $"{currentSide} ended their turn."
            );

            GameManager.Instance.EffectManager.TriggerTurnEnd(currentSide);

            foreach (PlayerSide side in new[] { PlayerSide.Player, PlayerSide.Opponent })
            {
                var field = GameManager.Instance.GetBattlefield(side);
                if (field == null) continue;
                foreach (var unit in new System.Collections.Generic.List<RuntimeCard>(field.Minions))
                {
                    unit.FinishAbilityTurn(currentSide);
                    field.RefreshMinionView(unit);
                    GameManager.Instance.CombatManager.CheckDeath(unit);
                }
            }

            if (GameManager.Instance.IsGameOver)
                return;

            if (currentSide == PlayerSide.Player)
            {
                currentSide = PlayerSide.Opponent;
            }
            else
            {
                currentSide = PlayerSide.Player;
                turnNumber++;
            }

        }
        finally
        {
            endingTurn = false;
        }
        StartTurn();
    }

        public bool IsMyTurn(PlayerSide side)
    {
        return currentSide == side;
    }

    public int GetCurrentMana()
    {
        if (currentSide == PlayerSide.Player)
            return playerMana;

        return opponentMana;
    }
        public int GetMaxMana(PlayerSide side)
    {
        if (side == PlayerSide.Player)
            return playerMaxMana;

        return opponentMaxMana;
    }

    public bool CanSpendMana(
        PlayerSide side,
        int amount)
    {
        if (amount < 0)
            return false;

        if (side == PlayerSide.Player)
            return playerMana >= amount;

        return opponentMana >= amount;
    }

    public bool SpendMana(
        PlayerSide side,
        int amount)
    {
        if (!CanSpendMana(side, amount))
            return false;

        if (side == PlayerSide.Player)
        {
            playerMana -= amount;
        }
        else
        {
            opponentMana -= amount;
        }

        Debug.Log(
            $"{side} spent {amount} mana. " +
            $"Remaining: {GetMana(side)}"
        );

        return true;
    }

    public int GetMana(PlayerSide side)
    {
        if (side == PlayerSide.Player)
            return playerMana;

        return opponentMana;
    }

/// <summary>Restores available mana, capped at this side's current maximum.</summary>
public int RestoreMana(PlayerSide side, int amount)
{
    if (amount <= 0) return 0;
    int before = GetMana(side);
    int restored = Mathf.Min(amount, Mathf.Max(0, GetMaxMana(side) - before));
    if (side == PlayerSide.Player) playerMana += restored;
    else opponentMana += restored;
    return restored;
}

public void IncreaseMaxMana(
    PlayerSide side,
    int amount,
    bool refillAddedMana = true)
{
    Debug.Log(
        $"IncreaseMaxMana CALLED: {side}, " +
        $"Amount={amount}, " +
        $"Before={GetMaxMana(side)}"
    );

    if (amount <= 0)
        return;

    if (side == PlayerSide.Player)
    {
        int oldMaxMana = playerMaxMana;

        playerMaxMana =
            Mathf.Min(
                playerMaxMana + amount,
                maxMana
            );

        if (refillAddedMana)
        {
            int actualIncrease =
                playerMaxMana - oldMaxMana;

            playerMana =
                Mathf.Min(
                    playerMana + actualIncrease,
                    playerMaxMana
                );
        }
    }
    else
    {
        int oldMaxMana = opponentMaxMana;

        opponentMaxMana =
            Mathf.Min(
                opponentMaxMana + amount,
                maxMana
            );

        if (refillAddedMana)
        {
            int actualIncrease =
                opponentMaxMana - oldMaxMana;

            opponentMana =
                Mathf.Min(
                    opponentMana + actualIncrease,
                    opponentMaxMana
                );
        }
    }

    Debug.Log(
        $"IncreaseMaxMana FINISHED: {side}, " +
        $"After={GetMaxMana(side)}"
    );
}
}