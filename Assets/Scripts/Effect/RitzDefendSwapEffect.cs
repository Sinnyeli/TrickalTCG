using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RitzDefendSwapEffect", menuName = "Card Effects/Dragon/RitzDefendSwapEffect")]
public class RitzDefendSwapEffect : CardEffect
{
    public void SwapBeforeDefense(RuntimeCard unit)
    {
        int attack = unit.GetAttack(), health = unit.GetMaxHealth();
        unit.AddModifier(new RuntimeModifier(health - attack, attack - health, true, unit));
        unit.RecalculateCurrentHealth();
        GameManager.Instance.GetBattlefield(unit.Owner)?.RefreshMinionView(unit);
        GameManager.Instance.CombatManager.CheckDeath(unit);
    }
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets) { }
}
