using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Elena Ally Buff")]
public class ElenaAllyBuffEffect : CardEffect
{
    [SerializeField] private bool buffElena;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || GameManager.Instance == null) return;
        var field = GameManager.Instance.GetBattlefield(source.Owner);
        if (field == null) return;
        var elenas = new List<RuntimeCard>();
        foreach (var unit in field.Minions) if (unit.CurrentHealth > 0 && unit.Data.CardID == "A_ELENA") elenas.Add(unit);
        if (elenas.Count == 0) return;
        var affected = buffElena ? elenas : new List<RuntimeCard> { source };
        foreach (var target in affected)
        {
            target.AddModifier(new RuntimeModifier(2, 2, true, source));
            if (buffElena) target.GrantRuntimeKeyword(CardKeyword.Taunt, 0);
            field.RefreshMinionView(target);
        }
    }
}
