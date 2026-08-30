using System.Collections.Generic;
using UnityEngine;

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


    private List<RuntimeCard> opponentHand = new List<RuntimeCard>(); // Temp List for OpponentHand. 
    [SerializeField] private BattlefieldManager opponentBattlefieldManager;
    [SerializeField] private TurnManager turnManager;

    [Header("Game Setup")]
    [SerializeField] private int startingHandSize = 0;

    public DeckManager DeckManager => deckManager;
    public HandManager HandManager => handManager;
    public TurnManager TurnManager => turnManager;
    public CombatManager CombatManager => combatManager;

    // Get; Return function because I keep forgetting 
    public BattlefieldManager PlayerBattlefieldManager =>
        playerBattlefieldManager;

    public BattlefieldManager OpponentBattlefieldManager =>
        opponentBattlefieldManager;

    [SerializeField]
    private DeckManager opponentDeck;

    private void Awake()
    {
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
        Debug.Log("Starting TCG Game...");

    // Player
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

            if (card == null)
            {
                Debug.Log(
                    $"{side} cannot draw. Deck is empty."
                );

                return;
            }

            card.SetOwner(side);

            bool added = hand.AddCard(card);

            if (!added)
            {
                deck.Discard(card);
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
    public void EndTurn()
    {
        turnManager.EndTurn();
    }

}