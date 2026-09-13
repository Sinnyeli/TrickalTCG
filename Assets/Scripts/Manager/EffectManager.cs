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
        if (card.IsSilenced)
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
        case EffectTargetType.Self:
            ResolveSelfTarget(source, effect);
            return;

        case EffectTargetType.EnemyHero:
        case EffectTargetType.FriendlyHero:
            ResolveHeroTarget(source, effect);
            return;

        // Unit targets require player selection
  
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
        case EffectTargetType.EnemyUnit:
        case EffectTargetType.FriendlyUnit:
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
    else if (effect is HealEffect healEffect)
    {
        healEffect.ResolveHero(source, target);
    }
}

    public void ResolveArtifactEffect(
        RuntimeCard artifact,
        CardEffect effect)
    {
        if (artifact == null || effect == null)
            return;

        ResolveEffect(artifact, effect);
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
            new List<RuntimeCard>();

        foreach (RuntimeCard minion in battlefield.Minions)
        {
            if (effect.MatchesTargetFilter(minion))
            {
                targets.Add(minion);
            }
        }

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
        new List<RuntimeCard>();

    foreach (RuntimeCard minion in battlefield.Minions)
    {
        if (effect.MatchesTargetFilter(minion))
        {
            targets.Add(minion);
        }
    }

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
        foreach (RuntimeCard minion in playerBattlefield.Minions)
        {
            if (effect.MatchesTargetFilter(minion))
            {
                targets.Add(minion);
            }
        }
    }

    if (opponentBattlefield != null)
    {
        foreach (RuntimeCard minion in opponentBattlefield.Minions)
        {
            if (effect.MatchesTargetFilter(minion))
            {
                targets.Add(minion);
            }
        }
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

    PlayerSide enemySide;

    if (source.Owner == PlayerSide.Player)
    {
        enemySide = PlayerSide.Opponent;
    }
    else
    {
        enemySide = PlayerSide.Player;
    }

    BattlefieldManager battlefield =
        GameManager.Instance.GetBattlefield(enemySide);

    if (battlefield == null)
        return;

    List<RuntimeCard> validTargets =
        new List<RuntimeCard>();

    foreach (RuntimeCard minion in battlefield.Minions)
    {
        if (effect.MatchesTargetFilter(minion))
        {
            validTargets.Add(minion);
        }
    }

    if (validTargets.Count == 0)
        return;

    int randomIndex =
        Random.Range(0, validTargets.Count);

    RuntimeCard target =
        validTargets[randomIndex];

    List<RuntimeCard> targets =
        new List<RuntimeCard>();

    targets.Add(target);

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

    List<RuntimeCard> validTargets =
        new List<RuntimeCard>();

    foreach (RuntimeCard minion in battlefield.Minions)
    {
        if (effect.MatchesTargetFilter(minion))
        {
            validTargets.Add(minion);
        }
    }

    if (validTargets.Count == 0)
        return;

    int randomIndex =
        Random.Range(0, validTargets.Count);

    RuntimeCard target =
        validTargets[randomIndex];

    List<RuntimeCard> targets =
        new List<RuntimeCard>();

    targets.Add(target);

    effect.Resolve(source, targets);
}

private void ResolveRandomUnit(
    RuntimeCard source,
    CardEffect effect)
{
    if (source == null || effect == null)
        return;

    BattlefieldManager playerBattlefield =
        GameManager.Instance.GetBattlefield(
            PlayerSide.Player
        );

    BattlefieldManager opponentBattlefield =
        GameManager.Instance.GetBattlefield(
            PlayerSide.Opponent
        );

    List<RuntimeCard> validTargets =
        new List<RuntimeCard>();

    if (playerBattlefield != null)
    {
        foreach (RuntimeCard minion in playerBattlefield.Minions)
        {
            if (effect.MatchesTargetFilter(minion))
            {
                validTargets.Add(minion);
            }
        }
    }

    if (opponentBattlefield != null)
    {
        foreach (RuntimeCard minion in opponentBattlefield.Minions)
        {
            if (effect.MatchesTargetFilter(minion))
            {
                validTargets.Add(minion);
            }
        }
    }

    if (validTargets.Count == 0)
        return;

    int randomIndex =
        Random.Range(0, validTargets.Count);

    RuntimeCard target =
        validTargets[randomIndex];

    List<RuntimeCard> targets =
        new List<RuntimeCard>();

    targets.Add(target);

    effect.Resolve(source, targets);
}

    private void ResolveSelfTarget(
        RuntimeCard source,
        CardEffect effect)
    {
        if (source == null || effect == null)
            return;

        List<RuntimeCard> targets =
            new List<RuntimeCard>
            {
                source
            };

        effect.Resolve(source, targets);
    }


    //////////////////
    /// Spell
    //////////////////

    public void ResolveSpell(RuntimeCard card)
    {
        if (card == null || card.Data == null)
            return;

        if (!(card.Data is SpellData spellData))
            return;

        CardEffect effect = spellData.SpellEffect;

        if (effect == null)
        {
            Debug.Log(
                $"{card.Data.cardName} has no Spell Effect."
            );

            return;
        }

        ResolveEffect(card, effect);
    }

        public void ResolveResonance(RuntimeCard source)
    {
        if (source == null || source.Data == null)
            return;

        if (source.IsSilenced)
            return;

        CardEffect effect =
            source.Data.Resonance;

        if (effect == null)
            return;

        Debug.Log(
            $"{source.Data.cardName} activates Resonance."
        );

        ResolveEffect(
            source,
            effect
        );
    }
    public void TriggerResonance(PlayerSide side)
    {
        BattlefieldManager battlefield =
            GameManager.Instance.GetBattlefield(side);

        if (battlefield == null)
            return;

        List<RuntimeCard> minions =
            new List<RuntimeCard>(battlefield.Minions);

        foreach (RuntimeCard minion in minions)
        {
            if (minion == null)
                continue;

            if (minion.Data.Resonance == null)
                continue;

            ResolveResonance(minion);
        }
    }

    public void ResolveTurnStart(RuntimeCard source)
    {
        if (source == null || source.Data == null)
            return;

        if (source.IsSilenced)
            return;

        CardEffect effect =
            source.Data.TurnStart;

        if (effect == null)
            return;

        Debug.Log(
            $"{source.Data.cardName} activates Turn Start."
        );

        ResolveEffect(
            source,
            effect
        );
    }
    public void ResolveTurnEnd(RuntimeCard source)
    {
        if (source == null || source.Data == null)
            return;

        if (source.IsSilenced)
            return;

        CardEffect effect =
            source.Data.TurnEnd;

        if (effect == null)
            return;

        Debug.Log(
            $"{source.Data.cardName} activates Turn End."
        );

        ResolveEffect(
            source,
            effect
        );
    }
public void TriggerTurnStart(PlayerSide side)
{
    BattlefieldManager battlefield =
        GameManager.Instance.GetBattlefield(side);

    if (battlefield == null)
        return;

    List<RuntimeCard> minions =
        new List<RuntimeCard>(
            battlefield.Minions
        );

    foreach (RuntimeCard minion in minions)
    {
        if (minion == null)
            continue;

        if (minion.Zone != CardZone.Field)
            continue;

        ResolveTurnStart(minion);
    }
}
public void TriggerTurnEnd(PlayerSide side)
{
    BattlefieldManager battlefield =
        GameManager.Instance.GetBattlefield(side);

    if (battlefield == null)
        return;

    List<RuntimeCard> minions =
        new List<RuntimeCard>(
            battlefield.Minions
        );

    foreach (RuntimeCard minion in minions)
    {
        if (minion == null)
            continue;

        if (minion.Zone != CardZone.Field)
            continue;

        ResolveTurnEnd(minion);
    }
}
}