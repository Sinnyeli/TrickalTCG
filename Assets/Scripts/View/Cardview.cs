using System.Collections.Generic;
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
    public RuntimeCard RuntimeCard => runtimeCard;


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

    if (runtimeCard == null)
    {
        ReturnToHand();
        return;
    }

    // =====================================================
    // ARTIFACT → APOSTLE
    // =====================================================

    if (runtimeCard.Data is ArtifactData)
    {
        MinionView target =
            GetMinionUnderPointer(eventData);

        if (target != null)
        {
            bool equipped =
                target.TryEquipArtifact(runtimeCard);

            if (equipped)
                return;
        }

        ReturnToHand();
        return;
    }

    // =====================================================
    // MANUAL TARGET SPELL → UNIT
    // =====================================================

    if (runtimeCard.Data is SpellData spellData)
    {
        CardEffect effect =
            spellData.SpellEffect;

        if (effect != null)
        {
            switch (effect.TargetType)
            {
                case EffectTargetType.EnemyUnit:
                case EffectTargetType.FriendlyUnit:
                case EffectTargetType.AnyUnit:
                case EffectTargetType.AnyTarget:

                    MinionView targetView =
                        GetMinionUnderPointer(eventData);

                    if (targetView != null)
                    {
                        RuntimeCard targetCard =
                            targetView.RuntimeCard;

                        bool cast =
                            GameManager.Instance.HandManager
                                .PlayTargetedSpellFromHand(
                                    runtimeCard,
                                    targetCard
                                );

                        if (cast)
                            return;
                    }

                    ReturnToHand();
                    return;
            }
        }
    }

    // =====================================================
    // NORMAL CARD → BATTLEFIELD
    // =====================================================

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

    private MinionView GetMinionUnderPointer(
    PointerEventData eventData)
{
    List<RaycastResult> results =
        new List<RaycastResult>();

    EventSystem.current.RaycastAll(
        eventData,
        results
    );

    foreach (RaycastResult result in results)
    {
        MinionView minionView =
            result.gameObject.GetComponentInParent<MinionView>();

        if (minionView != null)
            return minionView;
    }

    return null;
}
    }