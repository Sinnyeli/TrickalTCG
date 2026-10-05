using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Levi Turn Study")]
public class LeviTurnStudyEffect : CardEffect
{
    [SerializeField] private ApostleData graduate;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        var game = GameManager.Instance;
        if (source == null || game == null || graduate == null || source.IsSilenced || source.Zone != CardZone.Field ||
            game.TurnManager == null || game.TurnManager.CurrentSide != source.Owner) return;
        int count = source.AdvanceTurnAbilityCounter(this);
        Debug.Log("Levi Study this turn: " + count + "/5");
        if (count != 5) return;
        var field = game.GetBattlefield(source.Owner);
        // Graduation may transform the same Commander runtime card; ordinary transform rules remain unchanged.
        if (field != null && field.TransformCard(source, graduate, true))
            game.GrantPermanentSpellDiscount(source.Owner, 1, "LEVI_UPGRADE");
    }
}
