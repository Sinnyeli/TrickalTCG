using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DamageEffect",
    menuName = "Card Effects/Damage"
)]
public class DamageEffect : CardEffect
{
    [SerializeField] private int amount = 1;

    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
         if (source == null || targets == null)
            return;

        foreach (RuntimeCard target in targets)
        {
            if (target == null)
                continue;

            target.TakeDamage(amount);

            Debug.Log(
                $"{source.Data.cardName} dealt {amount} damage to " +
                $"{target.Data.cardName}."
            );

            GameManager.Instance
                .GetBattlefield(target.Owner)
                .RefreshMinionView(target);

            GameManager.Instance
                .CombatManager
                .CheckDeath(target);
        }
    }  

       // Hero targeting
    public void ResolveHero(
        RuntimeCard source,
        PlayerView target)
    {
        if (source == null || target == null)
            return;

        target.TakeDamage(amount);

        Debug.Log(
            $"{source.Data.cardName} dealt {amount} damage to " +
            $"{target.Side} Hero."
        );
    }
}