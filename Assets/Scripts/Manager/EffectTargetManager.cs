using UnityEngine;

public class EffectTargetManager : MonoBehaviour
{
    public static EffectTargetManager Instance { get; private set; }

    private CardEffect currentEffect;
    private RuntimeCard sourceCard;

    public bool IsSelectingTarget =>
        currentEffect != null;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartTargetSelection(
        RuntimeCard source,
        CardEffect effect)
    {
        if (source == null || effect == null)
            return;

        sourceCard = source;
        currentEffect = effect;

        Debug.Log(
            $"{source.Data.cardName} is selecting a target for " +
            $"{effect.name}."
        );
    }

    public void SelectTarget(RuntimeCard target)
    {
        if (!IsSelectingTarget)
            return;

        if (target == null)
            return;

        if (!IsValidTarget(target))
        {
            Debug.Log("Invalid effect target.");
            return;
        }

        Debug.Log(
            $"Selected {target.Data.cardName} as effect target."
        );

        currentEffect.Resolve(sourceCard);

        ClearTargetSelection();
    }

    private bool IsValidTarget(RuntimeCard target)
    {
        return true;
    }

    public void CancelTargetSelection()
    {
        if (!IsSelectingTarget)
            return;

        Debug.Log("Effect target selection cancelled.");

        ClearTargetSelection();
    }

    private void ClearTargetSelection()
    {
        currentEffect = null;
        sourceCard = null;
    }
}