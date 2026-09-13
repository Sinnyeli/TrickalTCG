using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [Header("Turn Settings")]
    [SerializeField] private int maxMana = 10;

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
    

    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
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

    GameManager.Instance.DrawCard(currentSide);
}


    public void EndTurn()
    {
        if (GameManager.Instance.IsGameOver)
        return;
        Debug.Log(
            $"{currentSide} ended their turn."
        );

        GameManager.Instance.EffectManager.TriggerTurnEnd(currentSide);

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
}