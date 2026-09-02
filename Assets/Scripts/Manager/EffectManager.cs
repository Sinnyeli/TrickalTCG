using UnityEngine;

public class EffectManager : MonoBehaviour
    {
    public void ResolveBattlecry(RuntimeCard card)
    {
        if (card == null || card.Data == null)
            return;

        CardEffect effect = card.Data.Battlecry;

        if (effect == null)
            return;

        DamageEffect damageEffect =
            effect as DamageEffect;

        if (damageEffect != null)
        {
            EffectTargetManager.Instance.StartTargetSelection(
                card,
                effect
            );

            return;
        }

        effect.Resolve(card);
    }

    public void ResolveDeathrattle(RuntimeCard card)
    {
        if (card == null || card.Data == null)
            return;

        if (card.Data.Deathrattle == null)
            return;

        card.Data.Deathrattle.Resolve(card);
    }

    public void ResolveDamage(RuntimeCard source, RuntimeCard target, int damage)
{
    if (source == null || target == null)
        return;

    target.TakeDamage(damage);

    Debug.Log(
        $"{source.Data.cardName} dealt " +
        $"{damage} damage to " +
        $"{target.Data.cardName}."
    );

    GameManager.Instance
        .GetBattlefield(target.Owner)
        .RefreshMinionView(target);

    // Check if target died.
    GameManager.Instance
        .CombatManager
        .CheckDeath(target);
}
}