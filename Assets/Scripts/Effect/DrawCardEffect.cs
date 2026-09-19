using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DrawCardEffect",
    menuName = "Card Effects/Draw Card"
)]
public class DrawCardEffect : CardEffect
{
    [Header("Draw Settings")]
    [SerializeField]
    private int amount = 1;

    [SerializeField]
    private DrawType drawType =
        DrawType.Top;

    public override void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets)
    {
        if (source == null)
            return;

        for (int i = 0; i < amount; i++)
        {
            switch (drawType)
            {
                case DrawType.Top:

                    GameManager.Instance.DrawCard(
                        source.Owner
                    );

                    break;

                case DrawType.Specific:

                    DrawSpecific(source);

                    break;

                case DrawType.RandomMatching:

                    DrawRandomMatching(source);

                    break;
            }
        }
    }

    private void DrawSpecific(
        RuntimeCard source)
    {
        if (SpecificCard == null)
            return;

        DeckManager deck =
            GameManager.Instance.GetDeck(
                source.Owner
            );

        if (deck == null)
            return;

        RuntimeCard card =
            deck.DrawSpecificCard(
                SpecificCard
            );

        if (card == null)
            return;

        GameManager.Instance.HandleDrawnCard(
            card,
            source.Owner
        );
    }

    private void DrawRandomMatching(
        RuntimeCard source)
    {
        DeckManager deck =
            GameManager.Instance.GetDeck(
                source.Owner
            );

        if (deck == null)
            return;

        RuntimeCard card =
            deck.DrawRandomMatchingCard(
                MatchesTargetFilter
            );

        if (card == null)
            return;

        GameManager.Instance.HandleDrawnCard(
            card,
            source.Owner
        );
    }
}