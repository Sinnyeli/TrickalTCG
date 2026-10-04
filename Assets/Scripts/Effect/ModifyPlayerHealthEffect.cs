using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ModifyPlayerHealthEffect", menuName = "Card Effects/ModifyPlayerHealth")]
public class ModifyPlayerHealthEffect : CardEffect
{
    [SerializeField] private int maximumHealthIncrease = 3;
    [SerializeField] private int healAmount = 3;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || GameManager.Instance == null) return;
        var player = GameManager.Instance.GetPlayerView(source.Owner);
        if (player == null) return;
        player.IncreaseMaximumHealth(Mathf.Max(0, maximumHealthIncrease));
        player.Heal(Mathf.Max(0, healAmount));
    }
}
