using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Destroy Low Attack")]
public class DestroyLowAttackEffect : CardEffect
{
    [SerializeField] private int maximumAttack = 3;
    public override bool CanTarget(RuntimeCard source, RuntimeCard target)
    {
        return source != null && target != null && target.Zone == CardZone.Field &&
            target.GetAttack() <= maximumAttack && base.CanTarget(source, target);
    }
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || targets == null || GameManager.Instance == null) return;
        foreach (RuntimeCard target in targets)
        {
            if (!CanTarget(source, target)) continue;
            target.Kill();
            GameManager.Instance.CombatManager.CheckDeath(target);
        }
    }
}
