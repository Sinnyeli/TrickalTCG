using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DamageEqualToDamageTakenEffect",
    menuName = "Card Effects/Damage Equal To Damage Taken"
)]
public class DamageEqualToDamageTakenEffect : CardEffect
{
    public override void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets)
    {
        if (source == null || targets == null)
            return;

        int amount = source.DamageTaken;

        if (amount <= 0)
            return;

        foreach (RuntimeCard target in targets)
        {
            if (target == null)
                continue;

            target.TakeDamage(amount);

            Debug.Log(
                $"Ayla Deathrattle | " +
                $"DamageTaken: {source.DamageTaken} | " +
                $"Target: {target.Data.cardName} | " +
                $"Target HP Before: {target.CurrentHealth} | " +
                $"Damage: {amount}"
            );

            GameManager.Instance
                .GetBattlefield(target.Owner)
                .RefreshMinionView(target);

            GameManager.Instance
                .CombatManager
                .CheckDeath(target);
        }
    }
}