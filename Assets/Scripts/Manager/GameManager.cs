using System.Collections.Generic;
using UnityEngine;

public enum GameState
{
    Loading,
    Playing,
    Paused,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Systems")]
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private HandManager handManager;
    [SerializeField] private BattlefieldManager playerBattlefieldManager;
    [SerializeField] private DeckManager opponentDeckManager;
    [SerializeField] private HandManager opponentHandManager;
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private EffectManager effectManager;



    [Header("Player Views")]
    [SerializeField] private GameObject playerViewPrefab;
    [SerializeField] private Transform playerPosition;
    [SerializeField] private Transform opponentPosition;


    [Header("Other UIs")]
    [SerializeField] private GameOverUI gameOverUI;

    private List<RuntimeCard> opponentHand = new List<RuntimeCard>(); // Temp List for OpponentHand. 
    [SerializeField] private BattlefieldManager opponentBattlefieldManager;
    [SerializeField] private TurnManager turnManager;

    [Header("Game Setup")]
    [SerializeField] private int startingHandSize = 0;
    private GameState currentGameState = GameState.Playing;

    public GameState CurrentGameState => currentGameState;

    private PlayerView playerView;
    private PlayerView opponentView;
    public DeckManager DeckManager => deckManager;
    public HandManager HandManager => handManager;
    public TurnManager TurnManager => turnManager;
    public CombatManager CombatManager => combatManager;
    public EffectManager EffectManager => effectManager;

    public bool IsGameOver =>
        currentGameState == GameState.GameOver;
    public bool IsPlayerTurn =>
    turnManager.IsMyTurn(PlayerSide.Player);
    public bool CanPlayerAct =>
        currentGameState == GameState.Playing &&
        IsPlayerTurn;

    // Get; Return function because I keep forgetting 
    public BattlefieldManager PlayerBattlefieldManager =>
        playerBattlefieldManager;

    public BattlefieldManager OpponentBattlefieldManager =>
        opponentBattlefieldManager;

    [SerializeField]
    private DeckManager opponentDeck;



private void Awake()
{
    Debug.Log(
        $"GameManager Awake | ID: {GetInstanceID()} | " +
        $"GameOverUI: {gameOverUI}"
    );

    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }

    Instance = this;
}

    private void Start()
    {
        StartGame();
    }

    private void StartGame()
    {
         gameOverUI.Hide();
    // Player
        CreatePlayerViews();
        
        deckManager.InitializeDeck();
        RuntimeCard playerCommander = deckManager.GetCommander();

        if (playerCommander != null)
        {
            playerCommander.SetOwner(PlayerSide.Player);
            handManager.AddCommander(playerCommander);
        }

            DrawStartingHand();
    // Opponent (Testing)

        opponentDeckManager.InitializeDeck();
        RuntimeCard opponentCommander = opponentDeckManager.GetCommander();

       if (opponentCommander != null)
        {
            opponentHandManager.AddCommander(opponentCommander);
            opponentCommander.SetOwner(PlayerSide.Opponent);
            // Add to list above
        }
        DrawCard(PlayerSide.Opponent);
    }
        public void SetGameState(GameState newState)
    {
        currentGameState = newState;

        Debug.Log(
            $"Game State changed to: {currentGameState}"
        );
    }

       
    private void DrawStartingHand()
    {
        for (int i = 0; i < startingHandSize; i++)
        {
            DrawCard(PlayerSide.Player);
        }
    }

        public void DrawCard(PlayerSide side)
        {
            DeckManager deck;
            HandManager hand;

            if (side == PlayerSide.Player)
            {
                deck = deckManager;
                hand = handManager;
            }
            else
            {
                deck = opponentDeckManager;
                hand = opponentHandManager;
            }

            RuntimeCard card = deck.DrawCard();
            

            if (card != null)
            {
                card.SetOwner(side);

            bool added = hand.AddCard(card);
            GameManager.Instance.EffectManager.ResolveOnDraw(card);

            if (!added)
            {
                deck.Discard(card);
            }

                return;
            }
            Debug.Log(
                    $"{side} cannot draw. Deck is empty."
                );
            
        }
//// //////////////////
///  Create Player Views
/// ///////////////////
/// 
private void CreatePlayerViews()
{
    CreatePlayerView(
        PlayerSide.Player,
        playerPosition
    );
    playerView.SetSide(PlayerSide.Player);

    CreatePlayerView(
        PlayerSide.Opponent,
        opponentPosition
    );
    opponentView.SetSide(PlayerSide.Opponent);
    }
    private void CreatePlayerView(PlayerSide side, Transform position)
    {
        GameObject viewObject =
            Instantiate(playerViewPrefab, position);

        RectTransform viewRect =
            viewObject.GetComponent<RectTransform>();

        RectTransform positionRect =
            position.GetComponent<RectTransform>();

        if (viewRect == null || positionRect == null)
        {
            Destroy(viewObject);
            return;
        }

        // Match the position marker.
        viewRect.anchoredPosition = Vector2.zero;
        viewRect.localRotation = Quaternion.identity;
        viewRect.localScale = Vector3.one;

        PlayerView view =
            viewObject.GetComponent<PlayerView>();

        if (view == null)
        {
            Debug.LogError(
                "PlayerView prefab is missing PlayerView component."
            );

            Destroy(viewObject);
            return;
        }

        view.SetSide(side);

        if (side == PlayerSide.Player)
        {
            playerView = view;
        }
        else
        {
            opponentView = view;
        }
    }

/////////////
/// Get Card/Battlefield
/// ////////



    public BattlefieldManager GetBattlefield(PlayerSide side)
    {
    switch (side)
    {
        case PlayerSide.Player:
            return PlayerBattlefieldManager;

        case PlayerSide.Opponent:
            return OpponentBattlefieldManager;

        default:
            return null;
    }
    }

    public DeckManager GetDeck(PlayerSide side)
    {
        if (side == PlayerSide.Player)
            return deckManager;

        return opponentDeckManager;
    }
        public PlayerView GetPlayerView(PlayerSide side)
    {
        if (side == PlayerSide.Player)
            return playerView;

        return opponentView;
    }

        public HandManager GetOpponentHand()
    {
        return opponentHandManager;
    }

    //////////////////////////
    /// Player Turn
    /// /////////////////////



       public bool EndPlayerTurn()
    {
        if (!CanPlayerAct)
        {
            Debug.Log("Player cannot end turn right now.");
            return false;
        }

        GameManager.Instance.EndPlayerTurn();
        return true;
    }
    //////////////////////////
    /// Win/Lose
    /// /////////////////////

     public void PlayerDefeated(PlayerSide defeatedSide)
        {
            if (IsGameOver)
                return;

            PlayerSide winner =
                defeatedSide == PlayerSide.Player
                    ? PlayerSide.Opponent
                    : PlayerSide.Player;

            SetGameState(GameState.GameOver);

            Debug.Log(
                $"{defeatedSide} has been defeated!"
            );

            Debug.Log(
                $"{winner} wins!"
            );

            gameOverUI.Show(winner);
        }
 
public HandManager GetHandManager(
    PlayerSide side)
{
    if (side == PlayerSide.Player)
        return handManager;

    return opponentHandManager;
}

////////////////////////////
    // Test Area. 
///////////////////////////
    public void TestOpponentPlay()
    {
        RuntimeCard card =
            opponentHandManager.GetFirstNormalCard();
            if (card == null)
            {
                Debug.Log("Opponent has no normal cards to play.");
                return;
            }

            Debug.Log(
                $"Opponent attempting to play {card.Data.cardName}"
            );

            bool played =
                opponentHandManager.PlayCardFromHand(card);

            if (!played)
            {
                Debug.Log(
                    $"Opponent could not play {card.Data.cardName}"
                );
            }
        }
    public void TestDraw()
    {
        DrawCard(PlayerSide.Player);
    }

}