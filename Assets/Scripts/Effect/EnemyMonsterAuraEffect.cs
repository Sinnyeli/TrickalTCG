using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Enemy Monster Aura")]
public class EnemyMonsterAuraEffect : CardEffect
{
    [SerializeField] private int attackAmount = -1;
    [SerializeField] private int healthAmount = -1;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || GameManager.Instance == null) return;
        var field = GameManager.Instance.GetBattlefield(source.Owner == PlayerSide.Player ? PlayerSide.Opponent : PlayerSide.Player);
        if (field == null) return;
        foreach (var unit in field.Minions)
            if (unit.Data is MonsterData)
                unit.AddModifier(new RuntimeModifier(attackAmount, healthAmount, true, source, true));
    }
}
