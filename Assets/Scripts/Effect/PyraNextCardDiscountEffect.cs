using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PyraNextCardDiscountEffect", menuName = "Card Effects/Dragon/PyraNextCardDiscountEffect")]
public class PyraNextCardDiscountEffect : CardEffect
{
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source != null && GameManager.Instance != null) GameManager.Instance.AddNextCardDiscount(source.Owner, 2);
    }
}
