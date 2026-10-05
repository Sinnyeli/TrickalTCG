using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BattleDeckLoader : MonoBehaviour
{
    [Header("Database")]
    [SerializeField]
    private CardDatabase cardDatabase;


    private DeckSaveData loadedDeck;

    private ApostleData commander;

    private readonly List<CardData>
        mainDeck =
            new List<CardData>();
    [SerializeField]
    private MatchSetup matchSetup;

    public DeckSaveData LoadedDeck =>
        loadedDeck;

    public ApostleData Commander =>
        commander;

    public IReadOnlyList<CardData> MainDeck =>
        mainDeck;

    [Header("Battle")]
    [SerializeField]
    private DeckManager playerDeckManager;

    [SerializeField]
    private GameManager gameManager;

    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        LoadPlayerDeck();
    }


    // =========================================================
    // LOAD PLAYER DECK
    // =========================================================

    public bool LoadPlayerDeck()
    {
        if (!LocalAccountSession.RequireLogin()) return false;
        mainDeck.Clear();

        loadedDeck = null;
        commander = null;


        // =========================================
        // SESSION CHECK
        // =========================================

        if (!BattleSession.HasPlayerDeck())
        {
            Debug.LogError(
                "BattleSession has no selected deck."
            );

            return false;
        }


        if (cardDatabase == null)
        {
            Debug.LogError(
                "BattleDeckLoader has no CardDatabase."
            );

            return false;
        }


        string deckID =
            BattleSession.PlayerDeckID;


        // =========================================
        // FIND SAVE FILE
        // =========================================

        string deckFolder =
            AccountDeckStorage.GetFolder();


        if (!Directory.Exists(deckFolder))
        {
            Debug.LogError(
                "Deck save folder does not exist."
            );

            return false;
        }


        string[] files =
            Directory.GetFiles(
                deckFolder,
                "*.json"
            );


        string deckFile = null;


        foreach (string file in files)
        {
            string json =
                File.ReadAllText(file);

            DeckSaveData data =
                JsonUtility.FromJson<DeckSaveData>(
                    json
                );


            if (data != null &&
                data.deckID == deckID)
            {
                deckFile = file;
                loadedDeck = data;

                break;
            }
        }


        if (loadedDeck == null)
        {
            Debug.LogError(
                $"Could not find deck with ID: " +
                $"{deckID}"
            );

            return false;
        }


        // =========================================
        // COMMANDER
        // =========================================

        CardData commanderCard =
            cardDatabase.GetCardByID(
                loadedDeck.commanderID
            );


        if (!(commanderCard
              is ApostleData loadedCommander))
        {
            Debug.LogError(
                $"Commander ID " +
                $"{loadedDeck.commanderID} " +
                $"is not a valid Apostle."
            );

            return false;
        }


        commander =
            loadedCommander;


        // =========================================
        // MAIN DECK
        // =========================================

        if (loadedDeck.cards != null)
        {
            foreach (
                DeckCardEntry entry
                in loadedDeck.cards)
            {
                if (entry == null)
                    continue;

                if (entry.count <= 0)
                    continue;


                CardData card =
                    cardDatabase.GetCardByID(
                        entry.cardID
                    );


                if (card == null)
                {
                    Debug.LogWarning(
                        $"Could not resolve card: " +
                        $"{entry.cardID}"
                    );

                    continue;
                }


                // If saved as:
                //
                // Jubee x3
                //
                // we need three CardData entries
                // in the battle deck.
                for (int i = 0;
                     i < entry.count;
                     i++)
                {
                    mainDeck.Add(
                        card
                    );
                }
            }
        }


        // =========================================
        // DEBUG
        // =========================================

        Debug.Log(
            $"Battle deck loaded: " +
            $"{loadedDeck.deckName}"
        );

        Debug.Log(
            $"Commander: " +
            $"{commander.cardName}"
        );

        Debug.Log(
            $"Main Deck: " +
            $"{mainDeck.Count} cards"
        );


    if (UnityRemoteMatch.IsGuest)
    {
        gameManager.PrepareRemotePresentation(); UnityRemoteMatch.Instance.BattleReady(cardDatabase); return true;
    }
    if (mainDeck.Count != 30)
    {
        Debug.LogError(
            $"Loaded player deck contains " +
            $"{mainDeck.Count} cards instead of 30."
        );

        return false;
    }playerDeckManager.InitializeDeck(
    commander,
    mainDeck
);


Debug.Log(
    $"Player battle deck initialized: " +
    $"{loadedDeck.deckName}"
);
// =========================================
// START BATTLE
// =========================================

if (gameManager == null)
{
    Debug.LogError(
        "BattleDeckLoader has no GameManager."
    );

    return false;
}

if (BattleSession.OpponentType == BattleOpponentType.RemotePlayer)
{
    if (matchSetup == null) matchSetup = FindFirstObjectByType<MatchSetup>();
    if (matchSetup == null || !matchSetup.SetupOpponent()) { Debug.LogError("Remote opponent setup is missing."); return false; }
}
gameManager.StartGame();
if (BattleSession.OpponentType == BattleOpponentType.RemotePlayer)
{
    gameManager.TurnManager.Initialize(); UnityRemoteMatch.Instance.BattleReady(cardDatabase);
}
else BattleActions.ResetMatch();

return true;
}
}
