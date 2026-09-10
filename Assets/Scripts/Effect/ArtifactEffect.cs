using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "ArtifactEffect",
    menuName = "Card Effects/Artifact Effect"
)]
public class ArtifactEffect : CardEffect
{
    [Header("Modifier")]
    [SerializeField] private int attackAmount;
    [SerializeField] private int healthAmount;

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
                    false,
                    source,
                    false
                );

            target.AddModifier(modifier);
        }
    }
}