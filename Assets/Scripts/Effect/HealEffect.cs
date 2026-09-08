using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "HealEffect",
    menuName = "Card Effects/Heal"
)]
public class HealEffect : CardEffect
{
    [SerializeField] private int amount = 1;

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

            target.Heal(amount);

            Debug.Log(
                $"{source.Data.cardName} healed " +
                $"{target.Data.cardName} for {amount}."
            );

            GameManager.Instance
                .GetBattlefield(target.Owner)
                .RefreshMinionView(target);
        }
    }

    public void ResolveHero(
        RuntimeCard source,
        PlayerView target)
    {
        if (source == null || target == null)
            return;

        target.Heal(amount);

        Debug.Log(
            $"{source.Data.cardName} healed " +
            $"{target.Side} Hero for {amount}."
        );
    }
}