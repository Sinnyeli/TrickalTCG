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
    private readonly Dictionary<PlayerSide, int> nextCardDiscounts = new Dictionary<PlayerSide, int>();
    private readonly Dictionary<PlayerSide, int> temporaryNextCardDiscounts = new Dictionary<PlayerSide, int>();
    public int GetNextCardDiscount(PlayerSide side) =>
        (nextCardDiscounts.TryGetValue(side, out int amount) ? amount : 0) +
        (temporaryNextCardDiscounts.TryGetValue(side, out int temporary) ? temporary : 0);
    public void AddTemporaryNextCardDiscount(PlayerSide side, int amount)
    {
        temporaryNextCardDiscounts.TryGetValue(side, out int previous);
        temporaryNextCardDiscounts[side] = previous + Mathf.Max(0, amount);
        GetHandManager(side)?.RefreshAllCardViews();
    }
    public void ExpireTemporaryNextCardDiscount(PlayerSide side)
    {
        temporaryNextCardDiscounts.Remove(side);
        GetHandManager(side)?.RefreshAllCardViews();
    }
    public void AddNextCardDiscount(PlayerSide side, int amount)
    {
        nextCardDiscounts.TryGetValue(side, out int previous);
        nextCardDiscounts[side] = previous + Mathf.Max(0, amount);
        GetHandManager(side)?.RefreshAllCardViews();
    }
    public int ConsumeNextCardDiscount(PlayerSide side)
    {
        int amount = GetNextCardDiscount(side); nextCardDiscounts.Remove(side); temporaryNextCardDiscounts.Remove(side); return amount;
    }

    private readonly Dictionary<PlayerSide, int> permanentSpellDiscounts = new Dictionary<PlayerSide, int>();
    private readonly HashSet<string> claimedSpellRewards = new HashSet<string>();
    public int GetPermanentSpellDiscount(PlayerSide side)
        => permanentSpellDiscounts.TryGetValue(side, out int amount) ? amount : 0;
    public void GrantPermanentSpellDiscount(PlayerSide side, int amount, string rewardID)
    {
        if (amount <= 0 || !claimedSpellRewards.Add(side + ":" + rewardID)) return;
        permanentSpellDiscounts[side] = GetPermanentSpellDiscount(side) + amount;
        GetHandManager(side)?.RefreshAllCardViews();
    }

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

    public PlayerSide WinnerSide { get; private set; }
    public void PrepareRemotePresentation() { gameOverUI.Hide(); CreatePlayerViews(); }
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



    public void StartGame()
    {
         gameOverUI.Hide();
    // Player
        CreatePlayerViews();
        
 
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
        int opponentStartingCards = BattleSession.OpponentType == BattleOpponentType.RemotePlayer ? startingHandSize : 1;
        for (int i = 0; i < opponentStartingCards; i++) DrawCard(PlayerSide.Opponent);
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

    RuntimeCard card =
        deck.DrawCard();

    if (card == null)
    {
        Debug.Log(
            $"{side} cannot draw. Deck is empty."
        );

        return;
    }

    HandleDrawnCard(
        card,
        side
    );
}

public void HandleDrawnCard(
    RuntimeCard card,
    PlayerSide side)
{
    if (card == null)
        return;

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

    card.SetOwner(side);

    bool added =
        hand.AddCard(card);

    EffectManager.ResolveOnDraw(card);

    if (!added)
    {
        deck.Discard(card);
    }
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

            WinnerSide = winner;
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

    private readonly Dictionary<PlayerSide, int> destinyCounters = new Dictionary<PlayerSide, int>();
    private readonly Dictionary<PlayerSide, int> destinyDrawnKinds = new Dictionary<PlayerSide, int>();
    public int GetDestinyCounter(PlayerSide side) => destinyCounters.TryGetValue(side, out int count) ? count : 0;
    public void ResetDestiny(PlayerSide side)
    {
        destinyCounters.Remove(side); destinyDrawnKinds.Remove(side);
    }
    public void RecordDestinyDraw(PlayerSide side, int kind)
    {
        if (IsGameOver || kind < 0 || kind > 2) return;
        destinyCounters[side] = GetDestinyCounter(side) + 1;
        destinyDrawnKinds.TryGetValue(side, out int mask);
        mask |= 1 << kind; destinyDrawnKinds[side] = mask;
        Debug.Log(side + " Destiny: " + destinyCounters[side]);
        if (mask == 7) PlayerDefeated(side == PlayerSide.Player ? PlayerSide.Opponent : PlayerSide.Player);
    }
    private readonly Dictionary<PlayerSide, SpellData> lastCastSpells = new Dictionary<PlayerSide, SpellData>();
    public void RecordLastCastSpell(RuntimeCard card)
    {
        if (card != null && card.Data is SpellData spell) lastCastSpells[card.Owner] = spell;
    }
    public SpellData GetLastCastSpell(PlayerSide side) => lastCastSpells.TryGetValue(side, out var spell) ? spell : null;
    public void ResetSpellHistory(PlayerSide side) => lastCastSpells.Remove(side);
    public int GetWitchSpellAuraDiscount(PlayerSide side)
    {
        int amount = 0; var field = GetBattlefield(side);
        if (field == null) return 0;
        foreach (var unit in field.Minions)
        {
            if (unit.IsSilenced || unit.CurrentHealth <= 0 || !(unit.Data is MinionData data)) continue;
            foreach (var effect in data.Passives) if (effect is WitchSpellCostAuraEffect aura) amount += aura.Amount;
        }
        return amount;
    }
}