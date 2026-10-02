using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Permanent Spell Discount")]
public class PermanentSpellDiscountEffect : CardEffect
{
    [SerializeField, Min(1)] private int amount = 1;
    [Tooltip("Claimed once per player per game, even across multiple copies.")]
    [SerializeField] private string rewardID = "LEVI_UPGRADE";
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || string.IsNullOrWhiteSpace(rewardID)) return;
        GameManager.Instance?.GrantPermanentSpellDiscount(source.Owner, amount, rewardID);
    }
}
