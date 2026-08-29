using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Systems")]
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private HandManager handManager;
    [SerializeField]
    public BattlefieldManager playerBattlefieldManager;

    [SerializeField]
    public BattlefieldManager opponentBattlefieldManager;

    [Header("Game Setup")]
    [SerializeField] private int startingHandSize = 0;

    public DeckManager DeckManager => deckManager;
    public HandManager HandManager => handManager;
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

        deckManager.InitializeDeck();

        RuntimeCard commander =
            deckManager.GetCommander();

        if (commander != null)
        {
            handManager.AddCommander(commander);
        }

        DrawStartingHand();
    }

    private void DrawStartingHand()
    {
        for (int i = 0; i < startingHandSize; i++)
        {
            DrawCard();
        }
    }

    public void DrawCard()
    {
        RuntimeCard card =
            deckManager.DrawCard();

        if (card == null)
            return;

        bool added =
            handManager.AddCard(card);

        if (!added)
        {
            deckManager.Discard(card);
        }
    }

    // Test
    public void TestOpponentPlay()
{
    RuntimeCard card = opponentDeck.DrawCard();

    if (card == null)
    {
        Debug.Log("Opponent has no cards to play.");
        return;
    }

    bool success =
        opponentBattlefieldManager.PlayCard(card);

    if (!success)
    {
        Debug.Log("Opponent could not play the card.");
    }
}
}