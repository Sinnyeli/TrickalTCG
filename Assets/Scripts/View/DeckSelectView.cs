using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckSelectView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text deckNameText;
    [SerializeField] private TMP_Text commanderNameText;
    [SerializeField] private Image commanderArtwork;
    [SerializeField] private Button selectButton;

    private DeckSaveData deckData;
    private ApostleData commanderData;
    private DeckSelectManager manager;


    public DeckSaveData DeckData => deckData;


    // =========================================================
    // SETUP
    // =========================================================

    public void Setup(
        DeckSaveData data,
        ApostleData commander,
        DeckSelectManager deckSelectManager)
    {
        deckData = data;
        commanderData = commander;
        manager = deckSelectManager;

        RefreshView();

        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(
                SelectDeck
            );

            selectButton.onClick.AddListener(
                SelectDeck
            );
        }
    }


    // =========================================================
    // REFRESH
    // =========================================================

    private void RefreshView()
    {
        if (deckData == null)
            return;


        // Deck name is player-created,
        // so it should NOT be localized.
        if (deckNameText != null)
        {
            deckNameText.text =
                deckData.deckName;
        }


        if (commanderData == null)
            return;


        if (commanderArtwork != null)
        {
            commanderArtwork.sprite =
                commanderData.artwork;
        }


        RefreshLocalization();
    }


    // =========================================================
    // LOCALIZATION
    // =========================================================

    private void RefreshLocalization()
    {
        if (commanderData == null)
            return;

        if (commanderNameText == null)
            return;


        if (LocalizationManager.Instance != null)
        {
            commanderNameText.text =
                LocalizationManager.Instance
                    .GetCardName(
                        commanderData
                    );
        }
        else
        {
            commanderNameText.text =
                commanderData.cardName;
        }
    }


    // =========================================================
    // SELECT
    // =========================================================

    private void SelectDeck()
    {
        if (manager == null)
            return;

        if (deckData == null)
            return;


        manager.SelectDeck(
            deckData
        );
    }


    // =========================================================
    // LANGUAGE EVENT
    // =========================================================

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance
                .OnLanguageChanged +=
                RefreshLocalization;
        }
    }


    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance
                .OnLanguageChanged -=
                RefreshLocalization;
        }
    }
}