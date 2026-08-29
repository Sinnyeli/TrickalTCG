using UnityEngine;

public class MinionView : MonoBehaviour
{
    private RuntimeCard runtimeCard;

    public void Initialize(RuntimeCard card)
    {
        runtimeCard = card;

        Debug.Log($"Minion deployed: {card.Data.cardName}");
    }

    public RuntimeCard GetCard()
    {
        return runtimeCard;
    }
}