using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "BounceEffect",
    menuName = "Card Effects/Bounce"
)]
public class BounceEffect : CardEffect
{
    public override void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets)
    {
        if (source == null ||
            targets == null)
            return;

        List<RuntimeCard> targetsCopy =
            new List<RuntimeCard>(targets);

        foreach (RuntimeCard target in targetsCopy)
        {
            if (target == null)
                continue;

            if (target.Zone != CardZone.Field)
                continue;

            BattlefieldManager battlefield =
                GameManager.Instance.GetBattlefield(
                    target.Owner
                );

            if (battlefield == null)
                continue;

            bool bounced =
                battlefield.BounceCard(target);

            if (bounced)
            {
                Debug.Log(
                    $"{source.Data.cardName} returned " +
                    $"{target.Data.cardName} to hand."
                );
            }
        }
    }
}