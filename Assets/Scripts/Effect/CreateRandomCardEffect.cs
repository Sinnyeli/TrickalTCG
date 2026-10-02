using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Create Random Card", fileName = "CreateRandomCardEffect")]
public class CreateRandomCardEffect : CardEffect
{
    [SerializeField] private List<CardData> cardPool = new List<CardData>();
    [SerializeField, Min(1)] private int amount = 1;

    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || GameManager.Instance == null) return;
        var valid = cardPool.FindAll(card => card != null);
        if (valid.Count == 0) return;
        HandManager hand = GameManager.Instance.GetHandManager(source.Owner);
        if (hand == null) return;
        for (int i = 0; i < amount; i++)
            hand.AddGeneratedCard(valid[Random.Range(0, valid.Count)], source.Owner);
    }
}
