using UnityEngine;

public abstract class TriggerCondition
    : ScriptableObject
{
    public abstract bool IsMet(
        RuntimeCard source,
        RuntimeCard triggerCard,
        RuntimeModifier modifier
    );
}