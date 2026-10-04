using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RandomOutcomeEffect", menuName = "Card Effects/RandomOutcome")]
public class RandomOutcomeEffect : CardEffect
{
    [SerializeField] private List<CardEffect> outcomes = new List<CardEffect>();
    private bool resolving;
    public override void Resolve(RuntimeCard source, List<RuntimeCard> targets)
    {
        if (source == null || resolving || outcomes == null) return;
        var eligible = outcomes.FindAll(effect => effect != null && effect != this);
        if (eligible.Count == 0) { Debug.LogWarning(name + ": no random outcomes configured."); return; }
        resolving = true;
        try
        {
            // Children share the supplied unit selection; no new targeting prompt.
            eligible[Random.Range(0, eligible.Count)].Resolve(source, targets ?? new List<RuntimeCard>());
        }
        finally { resolving = false; }
    }
}
