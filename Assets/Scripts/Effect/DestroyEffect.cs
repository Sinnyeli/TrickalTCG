using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DestroyEffect",
    menuName = "Card Effects/Destroy"
)]
public class DestroyEffect : CardEffect
{
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

            if (target.Zone != CardZone.Field)
                continue;

            Debug.Log(
                $"{source.Data.cardName} destroys " +
                $"{target.Data.cardName}."
            );

            target.Kill();

            GameManager.Instance
                .CombatManager
                .CheckDeath(target);
        }
    }
}