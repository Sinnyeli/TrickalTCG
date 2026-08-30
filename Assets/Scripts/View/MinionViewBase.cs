using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public abstract class MinionViewBase : MonoBehaviour
{
[Header("UI")]
    public Image artworkImage;
    public TMP_Text nameText;
    public TMP_Text attackText;
    public TMP_Text healthText;
    public Image raceImage;

    protected RuntimeCard runtimeCard;
    public RuntimeCard RuntimeCard => runtimeCard;

    private bool isSelected;
    public bool IsSelected => isSelected;
    public void SetSelected(bool selected)
    {
        isSelected = selected; 
    }

    public void SetMinion(RuntimeCard card)
    {
        if (card == null)
        {
            Debug.LogError(
                "MinionView received a null RuntimeCard."
            );

            return;
        }

        runtimeCard = card;

        CardData data = card.Data;

        nameText.text = data.cardName;

        if (data.artwork != null)
            artworkImage.sprite = data.artwork;
        

        attackText.gameObject.SetActive(false);
        healthText.gameObject.SetActive(false);
        raceImage.gameObject.SetActive(false);
        

        if (data is MonsterData monster)
        {
            attackText.gameObject.SetActive(true);
            healthText.gameObject.SetActive(true);

            RefreshStats();
        }

        if (data is ApostleData apostle)
        {
            attackText.gameObject.SetActive(true);
            healthText.gameObject.SetActive(true);
            raceImage.gameObject.SetActive(true);

            RefreshStats();
            raceImage.sprite = apostle.typeIcon;
        }

        
        Debug.Log(
            $"MinionView created for {data.cardName}"
        );
        
    }
    public void RefreshStats()
{
    if (runtimeCard == null)
        return;

    if (attackText != null)
        attackText.text = runtimeCard.GetAttack().ToString();

    if (healthText != null)
        healthText.text = runtimeCard.CurrentHealth.ToString();
}


}
