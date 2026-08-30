using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MinionView : MinionViewBase, IPointerClickHandler
{
     public RuntimeCard runtimeCard;

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (runtimeCard == null)
            return;

        CombatManager combat =
            GameManager.Instance.CombatManager;

        if (combat.SelectedAttacker == null)
        {
            combat.SelectAttacker(runtimeCard);
        }
        else
        {
            combat.Attack(runtimeCard);
        }
    }

    
}