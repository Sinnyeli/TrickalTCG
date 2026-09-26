using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeckLoaderManager : MonoBehaviour
{
    private const int MaxDecks = 9;


    [Header("Database")]
    [SerializeField]
    private CardDatabase cardDatabase;


    [Header("Saved Deck UI")]
    [SerializeField]
    private Transform deckContent;

    [SerializeField]
    private SavedDeckView savedDeckPrefab;

    [SerializeField]
    private TMP_Text deckCountText;

    [SerializeField]
    private Button createDeckButton;


    private List<DeckSaveData> savedDecks =
        new List<DeckSaveData>();


    private void Start()
    {
        RefreshDeckList();
    }


    // =========================================================
    // REFRESH
    // =========================================================

public void RefreshDeckList()
{
    ClearDeckViews();

    savedDecks =
        LoadAllDecks();

    foreach (DeckSaveData deck
             in savedDecks)
    {
        if (deck == null)
            continue;

        SavedDeckView view =
            Instantiate(
                savedDeckPrefab,
                deckContent
            );

        view.Initialize(
            deck,
            this,
            cardDatabase
        );
    }

    // Always put Create New Deck
    // after all existing decks.
    if (createDeckButton != null)
    {
        createDeckButton.transform.SetAsLastSibling();
    }

    RefreshDeckCount();
}


    // =========================================================
    // LOAD ALL DECKS
    // =========================================================

    private List<DeckSaveData> LoadAllDecks()
    {
        List<DeckSaveData> decks =
            new List<DeckSaveData>();


        string folder =
            Path.Combine(
                Application.persistentDataPath,
                "Decks"
            );


        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);

            return decks;
        }


        string[] files =
            Directory.GetFiles(
                folder,
                "*.json"
            );


        foreach (string file in files)
        {
            try
            {
                string json =
                    File.ReadAllText(file);


                DeckSaveData data =
                    JsonUtility.FromJson<
                        DeckSaveData
                    >(json);


                if (data == null)
                    continue;


                if (string.IsNullOrWhiteSpace(
                        data.deckID))
                {
                    Debug.LogWarning(
                        $"Deck file has no Deck ID: {file}"
                    );

                    continue;
                }


                decks.Add(data);
            }
            catch (System.Exception exception)
            {
                Debug.LogError(
                    $"Failed to load deck file " +
                    $"{file}: " +
                    $"{exception.Message}"
                );
            }
        }


        return decks;
    }


    // =========================================================
    // CREATE
    // =========================================================

    public void CreateNewDeck()
    {
        if (savedDecks.Count >= MaxDecks)
        {
            Debug.Log(
                "Maximum number of decks reached."
            );

            return;
        }


        DeckEditorSession.StartNewDeck();


        SceneManager.LoadScene(
            "DeckEditor"
        );
    }




    // =========================================================
    // EDIT
    // =========================================================

    public void EditDeck(
        string deckID)
    {
        if (string.IsNullOrWhiteSpace(deckID))
            return;


        DeckEditorSession.EditDeck(
            deckID
        );


        SceneManager.LoadScene(
            "DeckEditor"
        );
    }


    // =========================================================
    // DELETE
    // =========================================================

    public void DeleteDeck(
        string deckID)
    {
        if (string.IsNullOrWhiteSpace(deckID))
            return;


        string folder =
            Path.Combine(
                Application.persistentDataPath,
                "Decks"
            );


        string path =
            Path.Combine(
                folder,
                deckID + ".json"
            );


        if (!File.Exists(path))
        {
            Debug.LogWarning(
                $"Could not find deck file: {deckID}"
            );

            return;
        }


        File.Delete(path);


        Debug.Log(
            $"Deleted deck: {deckID}"
        );


        RefreshDeckList();
    }


    // =========================================================
    // COUNT
    // =========================================================

    private void RefreshDeckCount()
    {
        int count =
            savedDecks.Count;


        if (deckCountText != null)
        {
            deckCountText.text =
                $"{count} / {MaxDecks}";
        }


        if (createDeckButton != null)
        {
            createDeckButton.gameObject.SetActive(
                count < MaxDecks
            );
        }
    }


    // =========================================================
    // CLEAR UI
    // =========================================================

private void ClearDeckViews()
{
    if (deckContent == null)
        return;

    foreach (Transform child in deckContent)
    {
        // Don't destroy the Create Deck button.
        if (createDeckButton != null &&
            child.gameObject ==
            createDeckButton.gameObject)
        {
            continue;
        }

        Destroy(child.gameObject);
    }
}


    // =========================================================
    // BACK
    // =========================================================

    public void BackToTitle()
    {
        SceneManager.LoadScene(
            "TitleScene"
        );
    }

    
}