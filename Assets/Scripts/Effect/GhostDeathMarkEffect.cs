using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Ghost Death Mark")]
public class GhostDeathMarkEffect : CardEffect
{
    [SerializeField] private bool destroyMarked;
    [SerializeField] private GhostDeathMarkEffect markingEffect;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null) return;
        if (!destroyMarked)
        {
            if (targets != null && targets.Count > 0) source.SetDeathMark(this, targets[0]);
            return;
        }
        if (markingEffect == null) return;
        var marked = source.ConsumeDeathMark(markingEffect);
        if (marked == null || marked.Zone != CardZone.Field || marked.CurrentHealth <= 0) return;
        marked.Kill(source); GameManager.Instance.CombatManager.CheckDeath(marked);
    }
}
