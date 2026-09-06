using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DrawCardEffect",
    menuName = "Card Effects/Draw Card"
)]
public class DrawCardEffect : CardEffect
{
    [SerializeField] private int amount = 1;

    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null)
            return;

        for (int i = 0; i < amount; i++)
        {
            GameManager.Instance.DrawCard(source.Owner);
        }
    }
}