using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CardTrigger
{
    None,
    TurnStart,
    TurnEnd,
    OnDamageTaken,
    OnAttack,
    OnDraw,
    OnDeath,
    OnPlay
}