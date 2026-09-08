using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "PassiveEffect",
    menuName = "Card Effects/Passive"
)]
public class PassiveEffect : CardEffect
{
    [SerializeField] private int attackAmount = 1;
    [SerializeField] private int healthAmount = 1;
    [SerializeField] private bool affectSelf = false;

    public override void Resolve(
        RuntimeCard source,
        List<RuntimeCard> targets)
    {
        if (source == null || targets == null)
            return;

        foreach (RuntimeCard target in targets)
        {
            Apply(source, target);
        }
    }

    public void Apply(
        RuntimeCard source,
        RuntimeCard target)
    {
        if (source == null || target == null)
            return;

        if (!affectSelf && target == source)
            return;

        RuntimeModifier modifier =
            new RuntimeModifier(
                attackAmount,
                healthAmount,
                false,
                source,
                true
            );

        target.AddModifier(modifier);
    }
}