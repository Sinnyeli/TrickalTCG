using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MinionView : MonoBehaviour
{
    [Header("UI")]
    public Image artworkImage;
    public TMP_Text nameText;
    public TMP_Text attackText;
    public TMP_Text healthText;
    public Image raceImage;

    private RuntimeCard runtimeCard;

    public RuntimeCard RuntimeCard => runtimeCard;

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

        
        Debug.Log(
            $"MinionView created for {data.cardName}"
        );
    }

    
}