using UnityEngine;

[CreateAssetMenu(
    fileName = "DamageEffect",
    menuName = "Card Effects/Damage"
)]
public class DamageEffect : CardEffect
{
    [SerializeField] private int damage = 1;

    [SerializeField] private EffectTarget targetType =
        EffectTarget.EnemyUnit;

    public EffectTarget TargetType => targetType;

    public override void Resolve(RuntimeCard source)
    {
        Debug.Log(
            $"{source.Data.cardName} wants to deal " +
            $"{damage} damage to {targetType}."
        );
    }
}