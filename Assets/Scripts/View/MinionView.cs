using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MinionView : MinionViewBase, IPointerClickHandler, IDropHandler
{

    private Vector3 originalScale;
        private void Awake()
    {
        originalScale = transform.localScale;
    }


    public void OnPointerClick(
            PointerEventData eventData)
        {
            // If we have effect manager + effect manager is selecting target.
            if (EffectTargetManager.Instance != null &&
                EffectTargetManager.Instance.IsSelectingTarget)
            {
                EffectTargetManager.Instance.SelectTarget(RuntimeCard);
                return;
            }

            //Otherwise it's just click minion.
            Debug.Log(
                $"Clicked Minion: " +
                $"{runtimeCard?.Data.cardName}"
            );

            if (runtimeCard == null)
            {
                Debug.LogError(
                    "MinionView has no RuntimeCard!"
                );

                return;
            }

            CombatManager combat =
                GameManager.Instance.CombatManager;

            if (combat == null)
            {
                Debug.LogError(
                    "CombatManager is missing!"
                );

                return;
            }

            if (combat.SelectedAttacker == null)
            {
                combat.SelectAttacker(RuntimeCard);
            }
            else
            {
                combat.Attack(RuntimeCard);
            }
        }

public void OnDrop(PointerEventData eventData)
{
    if (runtimeCard == null)
        return;

    CardView draggedCard =
        eventData.pointerDrag?.GetComponent<CardView>();

    if (draggedCard == null)
        return;

    RuntimeCard artifact = draggedCard.RuntimeCard;

    if (artifact == null)
        return;

    if (!(artifact.Data is ArtifactData))
    {
        Debug.Log("Only Artifacts can be equipped to an Apostle.");
        return;
    }

    if (!(runtimeCard.Data is ApostleData))
    {
        Debug.Log("Artifacts can only be equipped to Apostles.");
        return;
    }

    if (runtimeCard.EquippedArtifacts.Count >= 3)
    {
        Debug.Log(
            $"{runtimeCard.Data.cardName} already has " +
            "3 artifacts."
        );
        return;
    }

    Debug.Log(
        $"Artifact {artifact.Data.cardName} dropped onto " +
        $"{runtimeCard.Data.cardName}."
    );

    // Actual equip will go here.
}
public bool TryEquipArtifact(RuntimeCard artifact)
{
    if (artifact == null)
        return false;

    if (!(artifact.Data is ArtifactData))
    {
        Debug.Log("Only Artifacts can be equipped.");
        return false;
    }

    if (!(runtimeCard.Data is ApostleData))
    {
        Debug.Log(
            $"{runtimeCard.Data.cardName} cannot equip Artifacts."
        );
        return false;
    }

    if (runtimeCard.Owner != artifact.Owner)
    {
        Debug.Log("You can only equip your own Artifacts.");
        return false;
    }

    if (runtimeCard.EquippedArtifacts.Count >= 3)
    {
        Debug.Log(
            $"{runtimeCard.Data.cardName} already has 3 Artifacts."
        );
        return false;
    }

    TurnManager turnManager =
        GameManager.Instance.TurnManager;

    if (turnManager == null)
        return false;

    if (!turnManager.IsMyTurn(artifact.Owner))
    {
        Debug.Log("You cannot equip an Artifact during the opponent's turn.");
        return false;
    }

    int cost = artifact.Data.manaCost;

    if (!turnManager.CanSpendMana(artifact.Owner, cost))
    {
        Debug.Log(
            $"{artifact.Data.cardName} cannot be equipped. " +
            "Not enough mana."
        );
        return false;
    }

    bool equipped =
        runtimeCard.EquipArtifact(artifact);

    if (!equipped)
        return false;

    turnManager.SpendMana(artifact.Owner, cost);

    GameManager.Instance.HandManager.RemoveCardFromHand(artifact);

    GameManager.Instance
        .GetBattlefield(runtimeCard.Owner)
        .RefreshMinionView(runtimeCard);

    Debug.Log(
        $"{artifact.Data.cardName} equipped to " +
        $"{runtimeCard.Data.cardName}."
    );

    return true;
}


        
}

