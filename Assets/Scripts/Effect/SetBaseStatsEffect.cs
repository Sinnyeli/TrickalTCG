using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "SetBaseStatsEffect",
    menuName = "Card Effects/Set Base Stats"
)]
public class SetBaseStatsEffect : CardEffect
{
    [Header("Base Stats")]
    [SerializeField] private int attack;
    [SerializeField] private int health;

    [Header("Passive")]
    [SerializeField] private bool isPassive;

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

            target.SetBaseStatOverride(
                attack,
                health,
                source,
                isPassive
            );
        }
    }
}