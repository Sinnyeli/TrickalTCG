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
    public Image artifactOne;
    public Image artifactTwo;
    public Image artifactThree;

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
            raceImage.sprite = apostle.TypeIcon;
        }
        artifactOne.gameObject.SetActive(false);
        artifactTwo.gameObject.SetActive(false);
        artifactThree.gameObject.SetActive(false);
        
        Debug.Log(
            $"MinionView created for {data.cardName}"
        );
        RefreshArtifacts();
        
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
public void RefreshArtifacts()
{
    ClearArtifactSlot(artifactOne);
    ClearArtifactSlot(artifactTwo);
    ClearArtifactSlot(artifactThree);

    if (runtimeCard == null)
        return;

    IReadOnlyList<RuntimeCard> artifacts =
        runtimeCard.EquippedArtifacts;

    for (int i = 0; i < artifacts.Count; i++)
    {
        RuntimeCard artifact = artifacts[i];

        if (artifact == null)
            continue;

        Image slot = GetArtifactSlot(i);

        if (slot == null)
            continue;

        slot.sprite = artifact.Data.artwork;
        slot.gameObject.SetActive(true);
    }
}
private void ClearArtifactSlot(Image slot)
{
    if (slot == null)
        return;

    slot.sprite = null;
    slot.gameObject.SetActive(false);
}

private Image GetArtifactSlot(int index)
{
    switch (index)
    {
        case 0:
            return artifactOne;

        case 1:
            return artifactTwo;

        case 2:
            return artifactThree;

        default:
            return null;
    }
}


}
