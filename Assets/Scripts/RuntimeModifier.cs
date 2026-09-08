using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RuntimeModifier
{
    public int AttackBonus { get; private set; }
    public int HealthBonus { get; private set; }
    public bool CanBeSilenced { get; private set; }
    public RuntimeCard Source { get; private set; }
    public bool IsPassive { get; private set; }

public RuntimeModifier(int attackBonus, int healthBonus, bool canBeSilenced, RuntimeCard source = null, bool isPassive = false)
{
    AttackBonus = attackBonus;
    HealthBonus = healthBonus;
    CanBeSilenced = canBeSilenced;
    Source = source;
    IsPassive = isPassive;
}
}


// Basically here to set what can be silence dand what can't. 