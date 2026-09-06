using UnityEngine;

public class EffectTarget
{
    public RuntimeCard Unit { get; private set; }
    public PlayerView Hero { get; private set; }

    public bool IsUnit => Unit != null;
    public bool IsHero => Hero != null;

    public EffectTarget(RuntimeCard unit)
    {
        Unit = unit;
        Hero = null;
    }

    public EffectTarget(PlayerView hero)
    {
        Unit = null;
        Hero = hero;
    }
}