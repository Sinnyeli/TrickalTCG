using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CreateCardsEffect",
    menuName = "Card Effects/Create Cards"
)]
public class CreateCardsEffect : CardEffect
{
    [SerializeField] private CardData cardToCreate;
    [SerializeField] private int amount = 1;

    public override void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets)
    {
        if (source == null || cardToCreate == null)
            return;

        HandManager handManager;

        if (source.Owner == PlayerSide.Player)
            handManager = GameManager.Instance.HandManager;
        else
            handManager = GameManager.Instance.GetOpponentHand();

        if (handManager == null)
            return;

        for (int i = 0; i < amount; i++)
        {
            bool added =
                handManager.AddGeneratedCard(
                    cardToCreate,
                    source.Owner
                );

            if (!added)
            {
                Debug.Log(
                    $"Could not create {cardToCreate.cardName}. " +
                    "Hand may be full."
                );
            }
        }

        Debug.Log(
            $"{source.Data.cardName} created " +
            $"{amount}x {cardToCreate.cardName}."
        );
    }
}