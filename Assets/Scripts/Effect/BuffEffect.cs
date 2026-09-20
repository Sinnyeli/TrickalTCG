using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BuffEffect",
    menuName = "Card Effects/Buff"
)]
public class BuffEffect : CardEffect
{
    [SerializeField] private int attackAmount = 1;
    [SerializeField] private int healthAmount = 1;

    public override void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets)
    {
        if (source == null || targets == null)
            return;

        foreach (RuntimeCard target in targets)
        {
            if (target == null)
                continue;

            RuntimeModifier modifier =
                new RuntimeModifier(
                    attackAmount,
                    healthAmount,
                    true,
                    source,
                    source.IsResolvingPassive
                );

            target.AddModifier(modifier);

            Debug.Log(
                $"{source.Data.cardName} buffed " +
                $"{target.Data.cardName} by " +
                $"+{attackAmount}/+{healthAmount}."
            );

            GameManager.Instance
                .GetBattlefield(target.Owner)
                .RefreshMinionView(target);
        }
    }
}