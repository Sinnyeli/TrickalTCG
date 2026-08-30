using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MinionView : MinionViewBase, IPointerClickHandler
{

    private Vector3 originalScale;
        private void Awake()
    {
        originalScale = transform.localScale;
    }


    public void OnPointerClick(
            PointerEventData eventData)
        {
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
}

