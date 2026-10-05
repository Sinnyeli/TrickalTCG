using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeckSelectManager : MonoBehaviour
{
    [Header("Database")]
    [SerializeField]
    private CardDatabase cardDatabase;


    [Header("Deck Grid")]
    [SerializeField]
    private Transform deckContent;

    [SerializeField]
    private GameObject deckSelectViewPrefab;
    [Header("Manage Decks")]
    [SerializeField]
    private GameObject createManageDeckButton;


    [Header("Selected Deck")]
    [SerializeField]
    private TMP_Text selectedDeckNameText;

    [SerializeField]
    private TMP_Text selectedCommanderNameText;

    [SerializeField]
    private Image selectedCommanderArtwork;


    [Header("Match")]
    [SerializeField]
    private Button findMatchButton;


    private DeckSaveData selectedDeck;

    private readonly List<DeckSelectView>
        deckViews =
            new List<DeckSelectView>();


    public DeckSaveData SelectedDeck =>
        selectedDeck;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (!LocalAccountSession.RequireLogin()) return;
        LoadSavedDecks();

        ClearSelectedDeck();
    }

    private void RefreshCreateManageButton()
    {
        if (createManageDeckButton == null)
            return;

        // Always place it after every generated deck.
        createManageDeckButton.transform.SetAsLastSibling();
    }
    // =========================================================
    // LOAD SAVED DECKS
    // =========================================================

    private void LoadSavedDecks()
    {
        if (!LocalAccountSession.RequireLogin()) return;
        ClearDeckViews();


        if (cardDatabase == null)
        {
            Debug.LogError(
                "DeckSelectManager has no CardDatabase."
            );

            return;
        }


        if (deckContent == null)
        {
            Debug.LogError(
                "DeckSelectManager has no Deck Content."
            );

            return;
        }


        if (deckSelectViewPrefab == null)
        {
            Debug.LogError(
                "DeckSelectView Prefab is not assigned."
            );

            return;
        }


        string deckFolder =
            AccountDeckStorage.GetFolder();


        if (!Directory.Exists(deckFolder))
        {
            Debug.Log(
                "No saved deck folder found."
            );

            return;
        }


        string[] files =
            Directory.GetFiles(
                deckFolder,
                "*.json"
            );


        foreach (string file in files)
        {
            LoadDeckView(file);
        }

        RefreshCreateManageButton();

    Debug.Log(
        $"DeckSelector loaded " +
        $"{deckViews.Count} decks."
    );
    }


    // =========================================================
    // LOAD ONE DECK
    // =========================================================

    private void LoadDeckView(
        string filePath)
    {
        try
        {
            string json =
                File.ReadAllText(
                    filePath
                );


            DeckSaveData deckData =
                JsonUtility.FromJson<DeckSaveData>(
                    json
                );


            if (deckData == null)
                return;


            CardData commanderCard =
                cardDatabase.GetCardByID(
                    deckData.commanderID
                );


            if (!(commanderCard
                  is ApostleData commander))
            {
                Debug.LogWarning(
                    $"Deck {deckData.deckName} " +
                    $"has invalid Commander: " +
                    $"{deckData.commanderID}"
                );

                return;
            }


            GameObject obj =
                Instantiate(
                    deckSelectViewPrefab,
                    deckContent
                );


            DeckSelectView view =
                obj.GetComponent<DeckSelectView>();


            if (view == null)
            {
                Debug.LogError(
                    "DeckSelectView Prefab is " +
                    "missing DeckSelectView."
                );

                Destroy(obj);

                return;
            }


            view.Setup(
                deckData,
                commander,
                this
            );


            deckViews.Add(
                view
            );
        }
        catch (System.Exception exception)
        {
            Debug.LogError(
                $"Failed to load deck file " +
                $"{filePath}: " +
                $"{exception.Message}"
            );
        }
    }


    // =========================================================
    // SELECT DECK
    // =========================================================

    public void SelectDeck(
        DeckSaveData deck)
    {
        if (deck == null)
            return;


        selectedDeck =
            deck;


        RefreshSelectedDeck();


        Debug.Log(
            $"Selected deck: " +
            $"{selectedDeck.deckName} " +
            $"[{selectedDeck.deckID}]"
        );
    }


    // =========================================================
    // SELECTED DECK DISPLAY
    // =========================================================

    private void RefreshSelectedDeck()
    {
        if (selectedDeck == null)
        {
            ClearSelectedDeck();
            return;
        }


        if (selectedDeckNameText != null)
        {
            selectedDeckNameText.text =
                selectedDeck.deckName;
        }


        CardData commanderCard =
            cardDatabase.GetCardByID(
                selectedDeck.commanderID
            );


        if (commanderCard
            is ApostleData commander)
        {
            if (selectedCommanderArtwork != null)
            {
                selectedCommanderArtwork.sprite =
                    commander.artwork;

                selectedCommanderArtwork
                    .gameObject
                    .SetActive(true);
            }


            if (selectedCommanderNameText != null)
            {
                if (LocalizationManager.Instance != null)
                {
                    selectedCommanderNameText.text =
                        LocalizationManager.Instance
                            .GetCardName(
                                commander
                            );
                }
                else
                {
                    selectedCommanderNameText.text =
                        commander.cardName;
                }
            }
        }


        if (findMatchButton != null)
        {
            findMatchButton.interactable =
                true;
        }
    }


    // =========================================================
    // CLEAR SELECTED DECK
    // =========================================================

    private void ClearSelectedDeck()
    {
        selectedDeck = null;


        if (selectedDeckNameText != null)
        {
            selectedDeckNameText.text =
                "";
        }


        if (selectedCommanderNameText != null)
        {
            selectedCommanderNameText.text =
                "";
        }


        if (selectedCommanderArtwork != null)
        {
            selectedCommanderArtwork.sprite =
                null;

            selectedCommanderArtwork
                .gameObject
                .SetActive(false);
        }


        if (findMatchButton != null)
        {
            findMatchButton.interactable =
                false;
        }
    }


    // =========================================================
    // CLEAR GRID
    // =========================================================

    private void ClearDeckViews()
    {
        foreach (
            DeckSelectView view
            in deckViews)
        {
            if (view != null)
            {
                Destroy(
                    view.gameObject
                );
            }
        }


        deckViews.Clear();
    }

    // =========================================================
// CREATE NEW DECK
// =========================================================

public void CreateNewDeck()
{
    // Clear any deck that may have previously been selected
    // for editing.
    DeckEditorSession.Clear();

    SceneManager.LoadScene(
        "DeckEditor"
    );
}


// =========================================================
// MY DECKS
// =========================================================

public void OpenMyDecks()
{
    SceneManager.LoadScene(
        "DeckLoader"
    );
}


// =========================================================
// DONE
// =========================================================

public void ReturnToTitle()
{
    SceneManager.LoadScene(
        "TitleScreen"
    );
}
// =========================================================
// START MATCH
// =========================================================

public void StartMatch()
{
    if (selectedDeck == null)
    {
        Debug.LogWarning(
            "Cannot start match: no deck selected."
        );

        return;
    }

    if (string.IsNullOrEmpty(
            selectedDeck.deckID))
    {
        Debug.LogError(
            "Selected deck has no Deck ID."
        );

        return;
    }


    UnityRemoteMatch.Get().FindMatch(selectedDeck, cardDatabase);

}


}