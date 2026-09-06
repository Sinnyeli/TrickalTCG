using System.Collections.Generic;
using UnityEngine;

public abstract class CardEffect : ScriptableObject
{
    [SerializeField]
    protected EffectTargetType targetType;

    public EffectTargetType TargetType => targetType;

    public abstract void Resolve(RuntimeCard source, List<RuntimeCard> targets);
}