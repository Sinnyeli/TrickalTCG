using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DamageRedirectEffect", menuName = "Card Effects/Damage Redirect")]
public class DamageRedirectEffect : CardEffect
{
    [SerializeField] private string protectedCardID = "A_CHLOE";

    public bool Protects(RuntimeCard protector, RuntimeCard target)
    {
        return protector != null && target != null && protector != target &&
            protector.Owner == target.Owner && protector.Zone == CardZone.Field &&
            target.Zone == CardZone.Field && target.CurrentHealth > 0 && protector.CurrentHealth > 0 &&
            !protector.IsSilenced && target.Data != null &&
            target.Data.CardID == protectedCardID;
    }

    // Damage interception is queried by RuntimeCard before health is changed.
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets) { }
}
