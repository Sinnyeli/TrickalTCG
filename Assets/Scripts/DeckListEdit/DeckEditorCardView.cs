using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeckEditorCardView :
    MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("UI")]
    [SerializeField] private Image artworkImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text manaCostText;

    private CardData cardData;
    private DeckEditorManager deckEditor;

    // Temporary visual copy used while dragging.
    private GameObject dragGhost;

    private RectTransform dragGhostRect;
    private Canvas rootCanvas;

    // Prevent a drag from also being interpreted as a click.
    private bool wasDragging;

    public CardData CardData => cardData;


    // =========================================================
    // INITIALIZE
    // =========================================================

    private void Awake()
    {
        rootCanvas =
            GetComponentInParent<Canvas>();
    }


    public void SetCard(CardData data)
    {
        cardData = data;

        if (cardData == null)
            return;

        deckEditor =
            FindFirstObjectByType<DeckEditorManager>();

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
    // CLICK
    // =========================================================

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (wasDragging)
        {
            wasDragging = false;
            return;
        }

        if (cardData == null ||
            deckEditor == null)
            return;

        deckEditor.OnLibraryCardClicked(
            cardData
        );
    }


    // =========================================================
    // BEGIN DRAG
    // =========================================================

    public void OnBeginDrag(
        PointerEventData eventData)
    {
        if (cardData == null)
            return;

        if (rootCanvas == null)
            return;

        wasDragging = true;

        CreateDragGhost();

        UpdateDragGhostPosition(
            eventData
        );
    }


    // =========================================================
    // DRAG
    // =========================================================

    public void OnDrag(
        PointerEventData eventData)
    {
        if (dragGhost == null)
            return;

        UpdateDragGhostPosition(
            eventData
        );
    }


    // =========================================================
    // END DRAG
    // =========================================================

    public void OnEndDrag(
        PointerEventData eventData)
    {
        if (deckEditor != null)
        {
            deckEditor.HandleCardDrop(
                cardData,
                eventData
            );
        }

        DestroyDragGhost();
    }


    // =========================================================
    // CREATE DRAG GHOST
    // =========================================================

    private void CreateDragGhost()
    {
        // Remove an old ghost just in case.
        DestroyDragGhost();

        dragGhost =
            Instantiate(
                gameObject,
                rootCanvas.transform
            );

        dragGhost.name =
            $"{cardData.cardName}_DragGhost";

        dragGhostRect =
            dragGhost.GetComponent<RectTransform>();


        // =========================================
        // DISABLE CARD SCRIPT
        // =========================================

        DeckEditorCardView ghostCardView =
            dragGhost.GetComponent<
                DeckEditorCardView
            >();

        if (ghostCardView != null)
        {
            ghostCardView.enabled = false;
        }


        // =========================================
        // IGNORE RAYCASTS
        // =========================================

        CanvasGroup canvasGroup =
            dragGhost.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                dragGhost.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;


        // Slight transparency so it feels
        // like a dragged preview.
        canvasGroup.alpha = 0.8f;


        // Keep it above the rest of the UI.
        dragGhost.transform.SetAsLastSibling();
    }


    // =========================================================
    // MOVE DRAG GHOST
    // =========================================================

    private void UpdateDragGhostPosition(
        PointerEventData eventData)
    {
        if (dragGhostRect == null)
            return;

        dragGhostRect.position =
            eventData.position;
    }


    // =========================================================
    // DESTROY DRAG GHOST
    // =========================================================

    private void DestroyDragGhost()
    {
        if (dragGhost != null)
        {
            Destroy(dragGhost);
        }

        dragGhost = null;
        dragGhostRect = null;
    }
}