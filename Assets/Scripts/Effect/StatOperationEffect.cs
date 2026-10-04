using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StatOperationEffect", menuName = "Card Effects/StatOperation")]
public class StatOperationEffect : CardEffect
{
    public enum Operation { Swap, Double, HalveAttack, CopySource }
    [SerializeField] private Operation operation = Operation.Swap;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || targets == null || GameManager.Instance == null) return;
        foreach (var target in new List<RuntimeCard>(targets))
        {
            if (target == null || target.Zone != CardZone.Field || target.CurrentHealth <= 0) continue;
            int oldAttack = target.GetAttack(), oldHealth = target.GetMaxHealth();
            int nextAttack = oldAttack, nextHealth = oldHealth;
            switch (operation)
            {
                case Operation.Swap: nextAttack = oldHealth; nextHealth = oldAttack; break;
                case Operation.Double: nextAttack = (int)System.Math.Min(int.MaxValue, (long)oldAttack * 2); nextHealth = (int)System.Math.Min(int.MaxValue, (long)oldHealth * 2); break;
                case Operation.HalveAttack: nextAttack = oldAttack / 2; break;
                case Operation.CopySource: nextAttack = source.GetAttack(); nextHealth = source.GetMaxHealth(); break;
            }
            target.AddModifier(new RuntimeModifier(nextAttack - oldAttack, nextHealth - oldHealth, true, source));
            target.RecalculateCurrentHealth(); // Retain wounds; operations use maximum health.
            GameManager.Instance.GetBattlefield(target.Owner)?.RefreshMinionView(target);
            GameManager.Instance.CombatManager.CheckDeath(target);
        }
    }
}
