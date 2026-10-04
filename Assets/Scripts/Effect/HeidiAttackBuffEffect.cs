using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Heidi Attack Buff")]
public class HeidiAttackBuffEffect : CardEffect
{
    public override bool CanTarget(RuntimeCard source, RuntimeCard target) => target != source && base.CanTarget(source, target);
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        foreach (var target in targets)
        {
            if (target == null || target == source) continue;
            target.AddModifier(new RuntimeModifier(2, 0, true, source));
            GameManager.Instance.GetBattlefield(target.Owner)?.RefreshMinionView(target);
        }
    }
}
