using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Ghost Copy Stats")]
public class GhostCopyStatsEffect : CardEffect
{
    public override bool CanTarget(RuntimeCard source, RuntimeCard target) => target != source && base.CanTarget(source, target);
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || targets == null || targets.Count == 0 || !CanTarget(source, targets[0])) return;
        source.SetCurrentStats(targets[0].GetAttack(), targets[0].CurrentHealth, source);
        GameManager.Instance.GetBattlefield(source.Owner)?.RefreshMinionView(source);
        GameManager.Instance.CombatManager.CheckDeath(source);
    }
}
