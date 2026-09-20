using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "TransformEffect",
    menuName = "Card Effects/Transform"
)]
public class TransformEffect : CardEffect
{
    [Header("Transform")]
    [SerializeField]
    private MinionData transformInto;

    public override void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets)
    {
        if (source == null ||
            transformInto == null ||
            targets == null)
            return;

        List<RuntimeCard> targetCopy =
            new List<RuntimeCard>(targets);

        foreach (RuntimeCard target in targetCopy)
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

            battlefield.TransformCard(
                target,
                transformInto
            );
        }
    }
}