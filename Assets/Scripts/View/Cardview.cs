using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CardView : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("UI")]
    public Image artworkImage;
    public TMP_Text nameText;
    public TMP_Text descriptionText;
    public TMP_Text costText;
    public TMP_Text attackText;
    public TMP_Text healthText;
    public Image raceImage;
    public TMP_Text typeText;

    public RuntimeCard runtimeCard;
    private HandManager handManager;
    private CanvasGroup canvasGroup;

    private bool isDragging = false;
    public bool IsDragging => isDragging;
   
   private BattlefieldDropZone battlefieldDropZone;
   
    private void Awake()
    {
        battlefieldDropZone = FindFirstObjectByType<BattlefieldDropZone>();
    }
    public void SetCard(RuntimeCard card)
    {
        runtimeCard = card;
        // get runtime card. Get data from it. 
        CardData data = card.Data;

        nameText.text = data.cardName;
        descriptionText.text = data.description;
        costText.text = data.manaCost.ToString();

        if (data.artwork != null)
            artworkImage.sprite = data.artwork;
        

        attackText.gameObject.SetActive(false);
        healthText.gameObject.SetActive(false);
        raceImage.gameObject.SetActive(false);
        typeText.gameObject.SetActive(false);
        

        if (data is MonsterData monster)
        {
            attackText.gameObject.SetActive(true);
            healthText.gameObject.SetActive(true);

            attackText.text = monster.attack.ToString();
            healthText.text = monster.health.ToString();
        }

        if (data is ApostleData apostle)
        {
            attackText.gameObject.SetActive(true);
            healthText.gameObject.SetActive(true);
            raceImage.gameObject.SetActive(true);

            attackText.text = apostle.attack.ToString();
            healthText.text = apostle.health.ToString();
            raceImage.sprite = apostle.typeIcon;
        }

    }

public void OnPointerClick(PointerEventData eventData)
{
    if (isDragging)
        return;

    if (runtimeCard == null)
        return;

   // GameManager.Instance.HandManager.PlayCardFromHand(runtimeCard);
}

//////////////////////////
// Temp Drag and Drop 
/////////////////////////
public void OnBeginDrag(PointerEventData eventData)
{
    isDragging = true;

    transform.SetAsLastSibling();
}

public void OnDrag(PointerEventData eventData)
{
    if (!isDragging)
        return;

    transform.position = eventData.position;
}

public void OnEndDrag(PointerEventData eventData)
{
        isDragging = false;

    bool droppedOnBattlefield =
        battlefieldDropZone != null &&
        battlefieldDropZone.IsPointerInside(eventData);

    if (droppedOnBattlefield)
    {
        bool played =
            GameManager.Instance.HandManager
                .PlayCardFromHand(runtimeCard);

        if (played)
            return;
    }

    // Either:
    // 1. Dropped outside battlefield
    // 2. Battlefield was full
    // 3. Card couldn't be played
    ReturnToHand();
}

private void ReturnToHand()
{
        HandManager handManager =
        GameManager.Instance.HandManager;

    if (runtimeCard.IsCommander)
    {
        handManager.RefreshCommanderLayout();
    }
    else
    {
        handManager.RefreshHandLayout();
    }
}
}