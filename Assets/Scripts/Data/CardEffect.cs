using UnityEngine;

public abstract class CardEffect : ScriptableObject
{
    public abstract void Resolve(RuntimeCard card);
}