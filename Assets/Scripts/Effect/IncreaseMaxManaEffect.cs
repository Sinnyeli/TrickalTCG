using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "IncreaseMaxManaEffect",
    menuName = "Card Effects/Increase Max Mana"
)]
public class IncreaseMaxManaEffect : CardEffect
{
    [Min(1)]
    [SerializeField]
    private int amount = 1;

    public override void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets)
    {
        if (source == null)
            return;

        TurnManager turnManager =
            GameManager.Instance.TurnManager;

        if (turnManager == null)
            return;

        turnManager.IncreaseMaxMana(
            source.Owner,
            amount,
            false
        );

        Debug.Log(
            $"{source.Data.cardName} increased " +
            $"{source.Owner}'s maximum mana by {amount}."
        );
    }
}