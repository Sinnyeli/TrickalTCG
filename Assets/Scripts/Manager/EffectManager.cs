using UnityEngine;
using System.Collections.Generic;

public class EffectManager : MonoBehaviour
{
//////////////////
/// Battlecry
/// //////////////


    public void ResolveBattlecry(RuntimeCard card)
    {
        if (card == null || card.Data == null)
            return;

        CardEffect effect = card.Data.Battlecry;

        if (effect == null)
            return;

        ResolveEffect(card, effect);
    }

//////////////////
/// Deathrattle
/// //////////////
    public void ResolveDeathrattle(RuntimeCard card)
    {
        if (card == null || card.Data == null)
            return;

        CardEffect effect = card.Data.Deathrattle;

        if (effect == null)
            return;

        ResolveEffect(card, effect);
    }


//////////////////
/// Resolve effect (for cards only)
/// //////////////
   private void ResolveEffect(
    RuntimeCard source,
    CardEffect effect)
{
    if (source == null || effect == null)
        return;

    switch (effect.TargetType)
    {
        // No target required
        case EffectTargetType.None:
            effect.Resolve(
                source,
                new List<RuntimeCard>()
            );
            return;

        // Hero targets are automatic
        case EffectTargetType.EnemyHero:
        case EffectTargetType.FriendlyHero:
            ResolveHeroTarget(source, effect);
            return;

        // Unit targets require player selection
        case EffectTargetType.EnemyUnit:
        case EffectTargetType.FriendlyUnit:
        case EffectTargetType.AllEnemyUnits:
            ResolveAllEnemyUnits(source, effect);
            return;
        case EffectTargetType.AllFriendlyUnits:
            ResolveAllFriendlyUnits(source, effect);
            return;
        case EffectTargetType.AllUnits:
            ResolveAllUnits(source, effect);
            return;
        case EffectTargetType.RandomEnemyUnit:
            ResolveRandomEnemyUnit(source, effect);
            return;
        case EffectTargetType.RandomFriendlyUnit:
            ResolveRandomFriendlyUnit(source, effect);
            return;

        case EffectTargetType.RandomUnit:
            ResolveRandomUnit(source, effect);
            return;

        // Any Target
        case EffectTargetType.AnyUnit:
        case EffectTargetType.AnyTarget:
        

            if (!EffectTargetManager.Instance.HasValidTarget(
                    source,
                    effect))
            {
                Debug.Log(
                    $"{effect.name} did not trigger because " +
                    "there are no valid targets."
                );

                return;
            }

            EffectTargetManager.Instance.StartTargetSelection(
                source,
                effect
            );

            return;

        
    }
}

//////////////////
/// Resolve effect specifically on hero target because PlayerView
/// //////////////

private void ResolveHeroTarget(
    RuntimeCard source,
    CardEffect effect)
{
    PlayerSide targetSide;

    if (effect.TargetType == EffectTargetType.EnemyHero)
    {
        targetSide =
            source.Owner == PlayerSide.Player
                ? PlayerSide.Opponent
                : PlayerSide.Player;
    }
    else
    {
        targetSide = source.Owner;
    }

    PlayerView target = GameManager.Instance
        .GetPlayerView(targetSide);

    if (target == null)
    {
        Debug.Log(
            $"{effect.name} did not trigger because " +
            "the Hero target could not be found."
        );

        return;
    }

    if (effect is DamageEffect damageEffect)
    {
        damageEffect.ResolveHero(
            source,
            target
        );
    }
}
//////////////////
/// Resolve effect specifically on all enemy units
/// //////////////

    private void ResolveAllEnemyUnits(
        RuntimeCard source,
        CardEffect effect)
    {
        if (source == null || effect == null)
            return;

        PlayerSide enemySide =
            source.Owner == PlayerSide.Player
                ? PlayerSide.Opponent
                : PlayerSide.Player;

        BattlefieldManager battlefield =
            GameManager.Instance.GetBattlefield(enemySide);

        if (battlefield == null)
            return;

        List<RuntimeCard> targets =
            new List<RuntimeCard>(battlefield.Minions);

        if (targets.Count == 0)
            return;

        effect.Resolve(source, targets);
    }
//////////////////
/// Resolve effect specifically on all friendly units
/// //////////////

    private void ResolveAllFriendlyUnits(
        RuntimeCard source,
        CardEffect effect)
    {
        if (source == null || effect == null)
            return;

        BattlefieldManager battlefield =
            GameManager.Instance.GetBattlefield(source.Owner);

        if (battlefield == null)
            return;

        List<RuntimeCard> targets =
            new List<RuntimeCard>(battlefield.Minions);

        if (targets.Count == 0)
            return;

        effect.Resolve(source, targets);
    }

    private void ResolveAllUnits(
    RuntimeCard source,
    CardEffect effect)
{
    if (source == null || effect == null)
        return;

    BattlefieldManager playerBattlefield =
        GameManager.Instance.GetBattlefield(PlayerSide.Player);

    BattlefieldManager opponentBattlefield =
        GameManager.Instance.GetBattlefield(PlayerSide.Opponent);

    List<RuntimeCard> targets =
        new List<RuntimeCard>();

    if (playerBattlefield != null)
    {
        targets.AddRange(playerBattlefield.Minions);
    }

    if (opponentBattlefield != null)
    {
        targets.AddRange(opponentBattlefield.Minions);
    }

    if (targets.Count == 0)
        return;

    effect.Resolve(source, targets);
}

private void ResolveRandomEnemyUnit(
    RuntimeCard source,
    CardEffect effect)
{
    if (source == null || effect == null)
        return;

    PlayerSide enemySide =
        source.Owner == PlayerSide.Player
            ? PlayerSide.Opponent
            : PlayerSide.Player;

    BattlefieldManager battlefield =
        GameManager.Instance.GetBattlefield(enemySide);

    if (battlefield == null)
        return;

    if (battlefield.Minions.Count == 0)
        return;

    int randomIndex =
        Random.Range(0, battlefield.Minions.Count);

    RuntimeCard target =
        battlefield.Minions[randomIndex];

    List<RuntimeCard> targets =
        new List<RuntimeCard>
        {
            target
        };

    effect.Resolve(source, targets);
}

private void ResolveRandomFriendlyUnit(
    RuntimeCard source,
    CardEffect effect)
{
    if (source == null || effect == null)
        return;

    BattlefieldManager battlefield =
        GameManager.Instance.GetBattlefield(source.Owner);

    if (battlefield == null)
        return;

    if (battlefield.Minions.Count == 0)
        return;

    int randomIndex =
        Random.Range(0, battlefield.Minions.Count);

    RuntimeCard target =
        battlefield.Minions[randomIndex];

    List<RuntimeCard> targets =
        new List<RuntimeCard>
        {
            target
        };

    effect.Resolve(source, targets);
}
private void ResolveRandomUnit(
    RuntimeCard source,
    CardEffect effect)
{
    if (source == null || effect == null)
        return;

    BattlefieldManager playerBattlefield =
        GameManager.Instance.GetBattlefield(PlayerSide.Player);

    BattlefieldManager opponentBattlefield =
        GameManager.Instance.GetBattlefield(PlayerSide.Opponent);

    List<RuntimeCard> candidates =
        new List<RuntimeCard>();

    if (playerBattlefield != null)
    {
        candidates.AddRange(playerBattlefield.Minions);
    }

    if (opponentBattlefield != null)
    {
        candidates.AddRange(opponentBattlefield.Minions);
    }

    if (candidates.Count == 0)
        return;

    int randomIndex =
        Random.Range(0, candidates.Count);

    RuntimeCard target =
        candidates[randomIndex];

    List<RuntimeCard> targets =
        new List<RuntimeCard>
        {
            target
        };

    effect.Resolve(source, targets);
}

}