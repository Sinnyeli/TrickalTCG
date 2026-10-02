using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Positional Buff", fileName = "PositionalBuffEffect")]
public class PositionalBuffEffect : CardEffect
{
    public enum Position { Leftmost, Center, Rightmost }
    [SerializeField] private Position position;
    [SerializeField] private int attackAmount = 1;
    [SerializeField] private int healthAmount = 1;

    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || GameManager.Instance == null) return;
        BattlefieldManager field = GameManager.Instance.GetBattlefield(source.Owner);
        if (field == null || field.Minions.Count == 0) return;
        int index = position == Position.Leftmost ? 0 : position == Position.Rightmost
            ? field.Minions.Count - 1 : (field.Minions.Count - 1) / 2;
        // An even-sized board uses the left of the two center units.
        RuntimeCard target = field.Minions[index];
        target.AddModifier(new RuntimeModifier(attackAmount, healthAmount, true, source));
        field.RefreshMinionView(target);
    }
}
