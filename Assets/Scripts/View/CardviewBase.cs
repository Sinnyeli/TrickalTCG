using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public abstract class CardviewBase : MonoBehaviour
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
            raceImage.sprite = apostle.TypeIcon;
        }

    }
    public void RefreshCardView()
    {
        if (runtimeCard == null)
            return;

        if (costText != null)
        {
            costText.text =
                runtimeCard.GetManaCost().ToString();
        }

        // Keep any other runtime-updating fields here later.
    }

}
