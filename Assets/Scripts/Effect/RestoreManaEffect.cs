using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RestoreManaEffect", menuName = "Card Effects/Restore Mana")]
public class RestoreManaEffect : CardEffect
{
    [SerializeField] private bool randomAmount = true;
    [Min(0), SerializeField] private int minimumAmount = 0;
    [Min(0), SerializeField] private int maximumAmount = 2;

    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || GameManager.Instance == null) return;
        TurnManager turns = GameManager.Instance.TurnManager;
        if (turns == null) return;
        int minimum = Mathf.Max(0, minimumAmount);
        int maximum = Mathf.Max(minimum, maximumAmount);
        // Guard the inclusive integer bound against overflow.
        maximum = Mathf.Min(maximum, int.MaxValue - 1);
        minimum = Mathf.Min(minimum, maximum);
        int amount = randomAmount ? Random.Range(minimum, maximum + 1) : maximum;
        turns.RestoreMana(source.Owner, amount);
    }
}
