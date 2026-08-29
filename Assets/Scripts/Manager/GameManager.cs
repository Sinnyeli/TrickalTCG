using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Systems")]
    [SerializeField] private DeckManager deckManager;
    [SerializeField] private HandManager handManager;

    [Header("Game Setup")]
    [SerializeField] private int startingHandSize = 0;

    private void Start()
    {
        StartGame();
    }

    private void StartGame()
    {
        Debug.Log("Starting TCG Game...");

         DeckManager deckManager =
        FindFirstObjectByType<DeckManager>();

        deckManager.InitializeDeck();

        HandManager handManager =
        FindFirstObjectByType<HandManager>();

         handManager.AddCommander(deckManager.GetCommander());
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
        RuntimeCard card = deckManager.DrawCard();

        if (card == null)
            return;

        bool added = handManager.AddCard(card);

        if (!added)
        {
            deckManager.Discard(card);
        }
    }
}