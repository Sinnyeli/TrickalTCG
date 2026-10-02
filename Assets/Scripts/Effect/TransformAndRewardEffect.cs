using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Transform And Reward")]
public class TransformAndRewardEffect : CardEffect
{
    [SerializeField] private MinionData transformInto;
    [SerializeField] private CardEffect reward;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || targets == null || transformInto == null || GameManager.Instance == null) return;
        foreach (var target in new List<RuntimeCard>(targets))
        {
            if (target == null || target.Zone != CardZone.Field) continue;
            var field = GameManager.Instance.GetBattlefield(target.Owner);
            if (field != null && field.TransformCard(target, transformInto))
                reward?.Resolve(source, new List<RuntimeCard> { target });
        }
    }
}
