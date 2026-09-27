using UnityEngine;

public abstract class CardEffectCondition : ScriptableObject
{
    public abstract bool IsMet(
        RuntimeCard source
    );
}