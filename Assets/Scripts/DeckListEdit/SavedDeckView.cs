using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SavedDeckView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField]
    private Image commanderArtwork;

    [SerializeField]
    private TMP_Text deckNameText;
  
    [SerializeField]
    private Button editButton;

    [SerializeField]
    private Button deleteButton;


    private DeckSaveData deckData;
    private DeckLoaderManager loaderManager;


    public void Initialize(
        DeckSaveData data,
        DeckLoaderManager manager,
        CardDatabase cardDatabase)
    {
        deckData = data;
        loaderManager = manager;


        if (deckData == null)
            return;


        // =========================================
        // NAME
        // =========================================

        if (deckNameText != null)
        {
            deckNameText.text =
                deckData.deckName;
        }

        // =========================================
        // COMMANDER ART
        // =========================================

        if (cardDatabase != null)
        {
            CardData commander =
                cardDatabase.GetCardByID(
                    deckData.commanderID
                );

            if (commander != null &&
                commanderArtwork != null)
            {
                commanderArtwork.sprite =
                    commander.artwork;
            }
        }


        // =========================================
        // BUTTONS
        // =========================================

        if (editButton != null)
        {
            editButton.onClick.RemoveAllListeners();

            editButton.onClick.AddListener(
                EditDeck
            );
        }


        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();

            deleteButton.onClick.AddListener(
                DeleteDeck
            );
        }
    }


    private int GetDeckSize()
    {
        if (deckData == null ||
            deckData.cards == null)
            return 0;


        int total = 0;

        foreach (DeckCardEntry entry
                 in deckData.cards)
        {
            if (entry == null)
                continue;

            total += entry.count;
        }

        return total;
    }


    private void EditDeck()
    {
        if (loaderManager == null ||
            deckData == null)
            return;

        loaderManager.EditDeck(
            deckData.deckID
        );
    }


    private void DeleteDeck()
    {
        if (loaderManager == null ||
            deckData == null)
            return;

        loaderManager.DeleteDeck(
            deckData.deckID
        );
    }
}