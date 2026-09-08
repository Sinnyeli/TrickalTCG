using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EffectTargetType
{
    None,

    Self,

    EnemyUnit,
    FriendlyUnit,
    AnyUnit,

    EnemyHero,
    FriendlyHero,

    AllEnemyUnits,
    AllFriendlyUnits,
    AllUnits,

    RandomEnemyUnit,
    RandomFriendlyUnit,
    RandomUnit,

    AnyTarget,
    RandomTarget
}