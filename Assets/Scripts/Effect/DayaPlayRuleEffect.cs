using UnityEngine;

[CreateAssetMenu(fileName = "DayaPlayRuleEffect", menuName = "Card Effects/Daya/Payment Form Rule")]
public class DayaPlayRuleEffect : ScriptableObject
{
    [SerializeField] private ApostleData baseForm;
    [SerializeField] private ApostleData pureShine;
    public int GetBaseCost(RuntimeCard card)
    {
        var turns = GameManager.Instance != null ? GameManager.Instance.TurnManager : null;
        return turns != null && turns.GetMana(card.Owner) >= 9 ? 9 : 6;
    }
    public bool ApplyBeforeHandPlay(RuntimeCard card)
    {
        if (card == null || card.Zone != CardZone.Hand || GameManager.Instance == null) return false;
        ApostleData form = GetBaseCost(card) == 9 ? pureShine : baseForm;
        return form != null && (card.Data == form || card.TransformInto(form));
    }
}
