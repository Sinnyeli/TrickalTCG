using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GrantKeywordEffect", menuName = "Card Effects/GrantKeyword")]
public class GrantKeywordEffect : CardEffect
{
    [SerializeField] private CardKeyword keyword = CardKeyword.Taunt;
    [Tooltip("0 = permanent; 1 = through the end of the current turn.")]
    [Min(0), SerializeField] private int turnEnds = 0;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || targets == null) return;
        foreach (var target in targets)
            if (target != null && target.Zone == CardZone.Field && target.CurrentHealth > 0)
                target.GrantRuntimeKeyword(keyword, Mathf.Max(0, turnEnds));
    }
}
