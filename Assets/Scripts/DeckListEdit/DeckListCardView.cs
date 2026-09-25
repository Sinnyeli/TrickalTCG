using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeckListCardView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text manaCostText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image artworkImage;

    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;

    [Header("Commander")]
    [SerializeField] private GameObject commanderLabel;

    private CardData cardData;
    private DeckEditorManager deckEditor;

    private bool isCommander;

    public CardData CardData => cardData;


    // =========================================================
    // NORMAL DECK CARD
    // =========================================================

    public void Initialize(
        CardData data,
        DeckEditorManager editor)
    {
        cardData = data;
        deckEditor = editor;
        isCommander = false;

        RefreshCardInfo();

        if (plusButton != null)
        {
            plusButton.gameObject.SetActive(true);

            plusButton.onClick.RemoveAllListeners();
            plusButton.onClick.AddListener(AddCopy);
        }

        if (minusButton != null)
        {
            minusButton.gameObject.SetActive(true);

            minusButton.onClick.RemoveAllListeners();
            minusButton.onClick.AddListener(RemoveCopy);
        }

        if (countText != null)
            countText.gameObject.SetActive(true);

        if (commanderLabel != null)
            commanderLabel.SetActive(false);

        Refresh();
    }


    // =========================================================
    // COMMANDER
    // =========================================================

    public void InitializeCommander(
        ApostleData data,
        DeckEditorManager editor)
    {
        cardData = data;
        deckEditor = editor;
        isCommander = true;

        RefreshCardInfo();

        // Commander has no +/- buttons.
        if (plusButton != null)
            plusButton.gameObject.SetActive(false);

        if (minusButton != null)
            minusButton.gameObject.SetActive(false);

        if (countText != null)
            countText.gameObject.SetActive(false);

        if (commanderLabel != null)
            commanderLabel.SetActive(true);
    }


    // =========================================================
    // CARD INFO
    // =========================================================

    private void RefreshCardInfo()
    {
        if (cardData == null)
            return;

        if (nameText != null)
            nameText.text = cardData.cardName;

        if (manaCostText != null)
            manaCostText.text =
                cardData.manaCost.ToString();

        if (artworkImage != null)
            artworkImage.sprite =
                cardData.artwork;
    }


    // =========================================================
    // ADD
    // =========================================================

    private void AddCopy()
    {
        if (isCommander)
            return;

        if (deckEditor == null ||
            cardData == null)
            return;

        deckEditor.TryAddCard(cardData);
    }


    // =========================================================
    // REMOVE
    // =========================================================

    private void RemoveCopy()
    {
        if (isCommander)
            return;

        if (deckEditor == null ||
            cardData == null)
            return;

        deckEditor.TryRemoveCard(cardData);
    }


    // =========================================================
    // REFRESH
    // =========================================================

    public void Refresh()
    {
        if (cardData == null)
            return;

        RefreshCardInfo();

        if (isCommander)
            return;

        if (deckEditor == null)
            return;

        int count =
            deckEditor.GetCardCount(cardData);

        if (countText != null)
            countText.text = count.ToString();
    }
}