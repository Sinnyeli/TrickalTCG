using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class CardView : CardviewBase,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    

    private bool isDragging = false;
    public bool IsDragging => isDragging;
    private BattlefieldDropZone battlefieldDropZone;



    private void Awake()
    {
        battlefieldDropZone = FindFirstObjectByType<BattlefieldDropZone>();
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
        if (runtimeCard == null)
            return;

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