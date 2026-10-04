using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MayoMerchandiseEffect", menuName = "Card Effects/Mayo/Create Merchandise")]
public class MayoMerchandiseEffect : CardEffect
{
    [SerializeField] private List<ArtifactData> merchandisePool = new List<ArtifactData>();
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || GameManager.Instance == null) return;
        var pool = merchandisePool.FindAll(c => c != null && !c.Collectible);
        if (pool.Count == 0) { Debug.LogWarning("Mayo has no configured non-collectible merchandise."); return; }
        GameManager.Instance.GetHandManager(source.Owner)?.AddGeneratedCard(pool[Random.Range(0, pool.Count)], source.Owner);
    }
}
