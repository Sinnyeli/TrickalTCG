using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System.IO;
using UnityEngine.SceneManagement;
public class DeckEditorManager : MonoBehaviour
{
    // =========================================================
    // DATABASE
    // =========================================================

    [Header("Card Database")]
    [SerializeField]
    private CardDatabase cardDatabase;

    // =========================================================
    // CURRENT DECK
    // =========================================================

    [Header("Current Deck")]
    [SerializeField]
    private Transform deckListContent;

    [SerializeField]
    private DeckListCardView deckListCardPrefab;

    [SerializeField]
    private TMP_Text deckCountText;

    [Header("Deck Information")]
    [SerializeField]
    private TMP_InputField deckNameInput;

    [SerializeField]
    private TMP_Text validationText;


    // =========================================================
    // COMMANDER
    // =========================================================
    [Header("Commander")]
    [SerializeField]
    private Transform commanderContent;

    private DeckListCardView commanderView;

    private ApostleData commander;

    public ApostleData Commander =>
        commander;


    // =========================================================
    // DECK DATA
    // =========================================================

    private const int RequiredDeckSize = 30;

    private Dictionary<CardData, int> deckCards =
        new Dictionary<CardData, int>();

    // =========================================================
    // DECK LIST UI
    // =========================================================

    private Dictionary<CardData, DeckListCardView>
        deckListViews =
            new Dictionary<CardData, DeckListCardView>();


    // =========================================================
    // COMMANDER SELECTION
    // =========================================================

    private bool selectingCommander = true;

    public bool IsSelectingCommander =>
        selectingCommander;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // Deck must begin without a Commander.
        commander = null;

        selectingCommander = true;
    

        RefreshDeckCount();

        Debug.Log(
            "Deck Editor started. " +
            "Select an Apostle as your Commander."
        );
    }

 
  

    // =========================================================
    // CARD CLICKED FROM LIBRARY
    // =========================================================

    public void OnLibraryCardClicked(
        CardData card)
    {
        if (card == null)
            return;

        // -----------------------------------------
        // COMMANDER SELECTION MODE
        // -----------------------------------------

        if (selectingCommander)
        {
            if (!(card is ApostleData apostle))
            {
                Debug.Log(
                    $"{card.cardName} cannot be Commander. " +
                    "Commander must be an Apostle."
                );

                return;
            }

            SetCommander(apostle);

            return;
        }

        // -----------------------------------------
        // NORMAL DECK BUILDING MODE
        // -----------------------------------------

        TryAddCard(card);
    }
   // =========================================================
    // SET NAME
    // =========================================================


    public string GetDeckName()
    {
        if (deckNameInput == null)
            return "New Deck";

        return deckNameInput.text.Trim();
    }

    // =========================================================
    // SET COMMANDER
    // =========================================================
public void SetCommander(
    ApostleData apostle)
{
    if (apostle == null)
        return;

    commander = apostle;

    selectingCommander = false;

    RefreshCommanderView();

    Debug.Log(
        $"{commander.cardName} selected as Commander."
    );
}


private void RefreshCommanderView()
{
    if (commander == null)
        return;

    if (commanderContent == null)
    {
        Debug.LogError(
            "Commander Content is not assigned."
        );

        return;
    }

    // If we already have a Commander view,
    // destroy the old one.
    if (commanderView != null)
    {
        Destroy(
            commanderView.gameObject
        );
    }

    // Reuse the exact same prefab as the deck list.
    commanderView =
        Instantiate(
            deckListCardPrefab,
            commanderContent
        );

    commanderView.InitializeCommander(
        commander,
        this
    );
}

    // =========================================================
    // CHANGE COMMANDER
    // =========================================================

    public void BeginCommanderSelection()
    {
        selectingCommander = true;

        Debug.Log(
            "Select a new Apostle Commander."
        );
    }


    // =========================================================
    // ADD CARD
    // =========================================================

    public bool TryAddCard(
        CardData card)
    {
        if (card == null)
            return false;


        // -----------------------------------------
        // COMMANDER REQUIRED FIRST
        // -----------------------------------------

        if (commander == null)
        {
            Debug.Log(
                "Select a Commander before building the deck."
            );

            return false;
        }


        // -----------------------------------------
        // HARD 30 CARD LIMIT
        // -----------------------------------------

        if (GetDeckSize() >= RequiredDeckSize)
        {
            Debug.Log(
                "Deck already contains 30 cards."
            );

            return false;
        }


        // -----------------------------------------
        // CURRENT COPY COUNT
        // -----------------------------------------

        int currentCount =
            GetCardCount(card);


        // -----------------------------------------
        // COPY LIMIT
        // -----------------------------------------

        int copyLimit =
            GetCopyLimit(card);

        if (copyLimit >= 0 &&
            currentCount >= copyLimit)
        {
            Debug.Log(
                $"{card.cardName} has reached " +
                $"its copy limit of {copyLimit}."
            );

            return false;
        }


        // -----------------------------------------
        // ADD CARD
        // -----------------------------------------

        if (deckCards.ContainsKey(card))
        {
            deckCards[card]++;
        }
        else
        {
            deckCards.Add(
                card,
                1
            );

            CreateDeckListView(card);
        }


        Debug.Log(
            $"Added {card.cardName}. " +
            $"Copies: {deckCards[card]}. " +
            $"Deck: {GetDeckSize()}/{RequiredDeckSize}"
        );


        RefreshDeckListView(card);

        RefreshDeckCount();

        return true;
    }


    // =========================================================
    // REMOVE CARD
    // =========================================================

    public bool TryRemoveCard(
        CardData card)
    {
        if (card == null)
            return false;

        if (!deckCards.ContainsKey(card))
            return false;


        deckCards[card]--;


        // -----------------------------------------
        // REMOVE ENTRY ENTIRELY
        // -----------------------------------------

        if (deckCards[card] <= 0)
        {
            deckCards.Remove(card);

            RemoveDeckListView(card);
        }
        else
        {
            RefreshDeckListView(card);
        }


        Debug.Log(
            $"Removed {card.cardName}. " +
            $"Deck: {GetDeckSize()}/{RequiredDeckSize}"
        );


        RefreshDeckCount();

        return true;
    }


    // =========================================================
    // GET CARD COUNT
    // =========================================================

    public int GetCardCount(
        CardData card)
    {
        if (card == null)
            return 0;

        if (!deckCards.TryGetValue(
                card,
                out int count))
        {
            return 0;
        }

        return count;
    }


    // =========================================================
    // GET DECK SIZE
    // =========================================================

    public int GetDeckSize()
    {
        int total = 0;

        foreach (
            KeyValuePair<CardData, int> pair
            in deckCards)
        {
            total += pair.Value;
        }

        return total;
    }


    // =========================================================
    // COPY LIMIT
    // =========================================================

    private int GetCopyLimit(
        CardData card)
    {
        if (card == null)
            return 0;


        // -----------------------------------------
        // JUBEE EXCEPTION
        // -----------------------------------------

        /*
         * Temporary identification by card name.
         *
         * Once we establish permanent Card IDs,
         * this should use cardID instead.
         */

        if (card.cardName == "Jubee")
        {
            // -1 means unlimited.
            return -1;
        }


        // -----------------------------------------
        // SPELL
        // -----------------------------------------

        if (card is SpellData)
        {
            return 3;
        }


        // -----------------------------------------
        // APOSTLE
        // -----------------------------------------

        if (card is ApostleData)
        {
            return 2;
        }


        // -----------------------------------------
        // MONSTER
        // -----------------------------------------

        if (card is MonsterData)
        {
            return 2;
        }


        // -----------------------------------------
        // ARTIFACT
        // -----------------------------------------

        if (card is ArtifactData)
        {
            return 2;
        }


        // Unknown card types cannot currently
        // be added to a deck.
        return 0;
    }


    // =========================================================
    // CREATE DECK LIST VIEW
    // =========================================================

    private void CreateDeckListView(
        CardData card)
    {
        if (card == null)
            return;

        if (deckListContent == null)
        {
            Debug.LogError(
                "Deck List Content is not assigned."
            );

            return;
        }

        if (deckListCardPrefab == null)
        {
            Debug.LogError(
                "Deck List Card Prefab is not assigned."
            );

            return;
        }


        DeckListCardView view =
            Instantiate(
                deckListCardPrefab,
                deckListContent
            );


        view.Initialize(
            card,
            this
        );


        deckListViews.Add(
            card,
            view
        );
    }


    // =========================================================
    // REMOVE DECK LIST VIEW
    // =========================================================

    private void RemoveDeckListView(
        CardData card)
    {
        if (card == null)
            return;


        if (!deckListViews.TryGetValue(
                card,
                out DeckListCardView view))
        {
            return;
        }


        if (view != null)
        {
            Destroy(
                view.gameObject
            );
        }


        deckListViews.Remove(card);
    }


    // =========================================================
    // REFRESH DECK LIST VIEW
    // =========================================================

    private void RefreshDeckListView(
        CardData card)
    {
        if (card == null)
            return;


        if (!deckListViews.TryGetValue(
                card,
                out DeckListCardView view))
        {
            return;
        }


        if (view == null)
            return;


        view.Refresh();
    }


    // =========================================================
    // REFRESH DECK COUNT
    // =========================================================

    private void RefreshDeckCount()
    {
        if (deckCountText == null)
            return;


        int deckSize =
            GetDeckSize();


        deckCountText.text =
            $"{deckSize} / {RequiredDeckSize}";
    }


    // =========================================================
    // DECK VALIDATION
    // =========================================================

    public bool IsDeckValid()
{
    // =========================================
    // DECK NAME
    // =========================================

    if (string.IsNullOrWhiteSpace(GetDeckName()))
        return false;


    // =========================================
    // COMMANDER
    // =========================================

    if (commander == null)
        return false;


    // =========================================
    // EXACTLY 30 CARDS
    // =========================================

    if (GetDeckSize() != RequiredDeckSize)
        return false;


    // =========================================
    // CARD VALIDATION
    // =========================================

    foreach (
        KeyValuePair<CardData, int> pair
        in deckCards)
    {
        CardData card = pair.Key;
        int count = pair.Value;

        if (card == null)
            return false;

        // Generated/token cards cannot
        // appear in constructed decks.
        if (!card.Collectible)
            return false;

        int limit =
            GetCopyLimit(card);

        // -1 means unlimited.
        if (limit >= 0 &&
            count > limit)
        {
            return false;
        }
    }

    return true;
}


    // =========================================================
    // DEBUG VALIDATION
    // =========================================================

    public void TestValidateDeck()
    {
        if (IsDeckValid())
        {
            Debug.Log(
                "Deck is VALID."
            );
        }
        else
        {
            Debug.Log(
                "Deck is NOT valid. " +
                $"Commander: " +
                $"{(commander != null ? commander.cardName : "None")}, " +
                $"Cards: {GetDeckSize()}/{RequiredDeckSize}"
            );
        }
    }

   public void HandleCardDrop(
    CardData card,
    PointerEventData eventData)
{
    if (card == null ||
        eventData == null)
        return;

    // eventData.hovered contains GameObjects
    // currently underneath the pointer.
    foreach (GameObject hoveredObject
             in eventData.hovered)
    {
        if (hoveredObject == null)
            continue;

        DeckEditorDropZone dropZone =
            hoveredObject.GetComponentInParent<
                DeckEditorDropZone
            >();

        if (dropZone == null)
            continue;


        // =========================================
        // COMMANDER DROP
        // =========================================

        if (dropZone.DropType ==
            DeckEditorDropType.Commander)
        {
            if (!(card is ApostleData apostle))
            {
                Debug.Log(
                    $"{card.cardName} cannot be Commander. " +
                    "Only Apostles can be Commanders."
                );

                return;
            }

            SetCommander(apostle);

            return;
        }


        // =========================================
        // DECK DROP
        // =========================================

        if (dropZone.DropType ==
            DeckEditorDropType.Deck)
        {
            TryAddCard(card);

            return;
        }
    }
}

// =========================================
// Create Saved Data (Deck)
// =========================================
private DeckSaveData CreateSaveData()
{
    DeckSaveData saveData =
        new DeckSaveData();

    saveData.deckName =
        GetDeckName();

    saveData.commanderID =
        commander.CardID;

    foreach (
        KeyValuePair<CardData, int> pair
        in deckCards)
    {
        DeckCardEntry entry =
            new DeckCardEntry(
                pair.Key.CardID,
                pair.Value
            );

        saveData.cards.Add(entry);
    }

    return saveData;
}

public bool SaveDeck()
{
    string validation =
        GetValidationMessage();

    if (!IsDeckValid())
    {
        Debug.LogWarning(validation);

        if (validationText != null)
        {
            validationText.text =
                validation;
        }

        return false;
    }


    DeckSaveData saveData =
        CreateSaveData();

    string json =
        JsonUtility.ToJson(
            saveData,
            true
        );


    string deckFolder =
        Path.Combine(
            Application.persistentDataPath,
            "Decks"
        );


    if (!Directory.Exists(deckFolder))
    {
        Directory.CreateDirectory(
            deckFolder
        );
    }


    string safeFileName =
        MakeSafeFileName(
            saveData.deckName
        );


    string path =
        Path.Combine(
            deckFolder,
            safeFileName + ".json"
        );


    try
    {
        File.WriteAllText(
            path,
            json
        );
    }
    catch (System.Exception exception)
    {
        Debug.LogError(
            $"Failed to save deck: " +
            $"{exception.Message}"
        );

        return false;
    }


    Debug.Log(
        $"Deck saved: {path}"
    );


    if (validationText != null)
    {
        validationText.text =
            $"Saved \"{saveData.deckName}\".";
    }


    return true;
}
private string GetValidationMessage()
{
    if (string.IsNullOrWhiteSpace(GetDeckName()))
    {
        return "Enter a deck name.";
    }

    if (commander == null)
    {
        return "Select an Apostle as your Commander.";
    }

    int deckSize =
        GetDeckSize();

    if (deckSize < RequiredDeckSize)
    {
        return
            $"Deck requires {RequiredDeckSize} cards. " +
            $"Currently: {deckSize}.";
    }

    if (deckSize > RequiredDeckSize)
    {
        return
            $"Deck cannot contain more than " +
            $"{RequiredDeckSize} cards.";
    }

    foreach (
        KeyValuePair<CardData, int> pair
        in deckCards)
    {
        CardData card = pair.Key;
        int count = pair.Value;

        if (card == null)
            return "Deck contains an invalid card.";

        if (!card.Collectible)
        {
            return
                $"{card.cardName} cannot be added to a deck.";
        }

        int limit =
            GetCopyLimit(card);

        if (limit >= 0 &&
            count > limit)
        {
            return
                $"{card.cardName} exceeds its " +
                $"copy limit of {limit}.";
        }
    }

    return "Deck is valid.";
}

private string MakeSafeFileName(
    string fileName)
{
    foreach (
        char invalidChar
        in Path.GetInvalidFileNameChars())
    {
        fileName =
            fileName.Replace(
                invalidChar,
                '_'
            );
    }

    return fileName;
}

public void DoneEditing()
{
    // =========================================
    // VALIDATE
    // =========================================

    string validation =
        GetValidationMessage();

    if (!IsDeckValid())
    {
        Debug.LogWarning(validation);

        if (validationText != null)
        {
            validationText.text =
                validation;
        }

        return;
    }


    // =========================================
    // SAVE
    // =========================================

    bool saved =
        SaveDeck();

    if (!saved)
    {
        Debug.LogError(
            "Deck could not be saved."
        );

        if (validationText != null)
        {
            validationText.text =
                "Failed to save deck.";
        }

        return;
    }


    // =========================================
    // RETURN TO DECK MENU
    // =========================================

    SceneManager.LoadScene(
        "DeckMenuScene"
    );
}
}