using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Ed Graduation")]
public class EdGraduationEffect : CardEffect
{
    [SerializeField] private SpellData farewell;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || source.IsSilenced || source.Zone != CardZone.Field || farewell == null) return;
        if (source.AdvanceAbilityCounter(this) == 3 && source.TryCompleteAbility(this))
            GameManager.Instance.GetHandManager(source.Owner)?.AddGeneratedCard(farewell, source.Owner);
    }
}
