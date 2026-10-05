using System;
using System.Collections.Generic;
using UnityEngine;

public class EffectTargetManager : MonoBehaviour
{
    public static EffectTargetManager Instance { get; private set; }

    private RuntimeCard sourceCard;
    private CardEffect currentEffect;
    

    public PlayerSide PendingOwner => UnityRemoteMatch.IsGuest || sourceCard == null ? PlayerSide.Player : sourceCard.Owner;
    public bool CanChoose(RuntimeCard card) => UnityRemoteMatch.IsGuest ? UnityRemoteMatch.Instance.CanChoose(card) : card != null && card.Zone == CardZone.Field && card.CurrentHealth > 0 && IsValidTarget(card);
    public bool CanChooseHero(PlayerView hero) => UnityRemoteMatch.IsGuest ? UnityRemoteMatch.Instance.CanChooseHero(hero) : hero != null && IsValidHeroTarget(hero);
    public bool CanChooseFor(RuntimeCard source, CardEffect effect, RuntimeCard card)
    {
        var oldSource = sourceCard; var oldEffect = currentEffect; sourceCard = source; currentEffect = effect;
        try { return CanChoose(card); } finally { sourceCard = oldSource; currentEffect = oldEffect; }
    }
    public bool CanChooseHeroFor(RuntimeCard source, CardEffect effect, PlayerView hero)
    {
        var oldSource = sourceCard; var oldEffect = currentEffect; sourceCard = source; currentEffect = effect;
        try { return CanChooseHero(hero); } finally { sourceCard = oldSource; currentEffect = oldEffect; }
    }
    public bool HasPendingTarget => currentEffect != null;
    public bool IsSelectingTarget => UnityRemoteMatch.IsGuest ? UnityRemoteMatch.Instance.TargetPrompt :
        HasPendingTarget && (BattleSession.OpponentType != BattleOpponentType.RemotePlayer || PendingOwner == PlayerSide.Player);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

//////////////////
/// Start Targeting
/// //////////////

    public void StartTargetSelection(
        RuntimeCard source,
        CardEffect effect,
        Action onResolved = null)
    {
        if (source == null || effect == null)
            return;

        sourceCard = source;
        currentEffect = effect;
        
        Debug.Log(
            $"{source.Data.cardName} is selecting a target."
        );
    }
//////////////////
/// Select Target
/// //////////////

    public void SelectTarget(RuntimeCard target)
    {
        if (!BattleActions.Executing && IsSelectingTarget) { BattleActions.Submit(PendingOwner, BattleActionKind.ChooseUnit, target: target); return; }
        if (!HasPendingTarget)
            return;

        if (target == null)
            return;

        if (!IsValidTarget(target))
        {
            Debug.Log("Invalid effect target.");
            return;
        }

        Debug.Log(
            $"Selected {target.Data.cardName}."
        );

        List<RuntimeCard> targets =
            new List<RuntimeCard>();

        targets.Add(target);

        currentEffect.Resolve(
            sourceCard,
            targets
        );
        ClearTargetSelection();
    }

//////////////////
/// Select PlayerView
/// //////////////

public void SelectHeroTarget(PlayerView target)
{
    if (!BattleActions.Executing && IsSelectingTarget && target != null) { BattleActions.Submit(PendingOwner, BattleActionKind.ChooseHero, hero: target.Side); return; }
    if (!HasPendingTarget)
        return;

    if (target == null)
        return;

    if (!IsValidHeroTarget(target))
    {
        Debug.Log("Invalid Hero target.");
        return;
    }

    Debug.Log(
        $"Selected {target.Side} Hero."
    );

    if (currentEffect is DamageEffect damageEffect)
    {
        damageEffect.ResolveHero(
            sourceCard,
            target
        );
    }
    else if (currentEffect is HealEffect healEffect)
    {
        healEffect.ResolveHero(sourceCard, target);
    }
    else return;

    ClearTargetSelection();
}



//////////////////
/// Check if the target selected is valid
/// //////////////

        private bool IsValidTarget(RuntimeCard target)
        {
            if (sourceCard == null || currentEffect == null)
                return false;

            bool validOwner = false;

            switch (currentEffect.TargetType)
            {
                case EffectTargetType.EnemyUnit:
                case EffectTargetType.EnemyTarget:
                    validOwner = target.Owner != sourceCard.Owner;
                    break;

                case EffectTargetType.FriendlyUnit:
                    validOwner = target.Owner == sourceCard.Owner;
                    break;

                case EffectTargetType.AnyUnit:
                case EffectTargetType.AnyTarget:
                    validOwner = true;
                    break;

                default:
                    return false;
            }

            if (!validOwner)
                return false;
            if (target.IsStealthed)
                return false;


            return currentEffect.CanTarget(sourceCard, target);
        }

        private bool IsValidHeroTarget(PlayerView target)
    {
        if (sourceCard == null || currentEffect == null)
            return false;

        switch (currentEffect.TargetType)
        {
            case EffectTargetType.EnemyHero:
            case EffectTargetType.EnemyTarget:
                return target.Side != sourceCard.Owner;

            case EffectTargetType.FriendlyHero:
                return target.Side == sourceCard.Owner;
            case EffectTargetType.AnyTarget:
            return true;

            default:
                return false;
        }
    }

    public void CancelTargetSelection()
    {
        ClearTargetSelection();
    }

    private void ClearTargetSelection()
    {
        sourceCard = null;
        currentEffect = null;
  
    }
    //////////////////
/// Check if there is any valid target to begin with.
/// //////////////

public bool HasValidTarget(
    RuntimeCard source,
    CardEffect effect)
{
    if (source == null || effect == null)
        return false;

    switch (effect.TargetType)
    {
        case EffectTargetType.EnemyTarget:
            return GameManager.Instance.GetPlayerView(GetOpposingSide(source.Owner)) != null
                || HasEnemyUnit(source, effect);
        case EffectTargetType.EnemyUnit:
            return HasEnemyUnit(source, effect);

        case EffectTargetType.FriendlyUnit:
            return HasFriendlyUnit(source, effect);

        case EffectTargetType.AnyUnit:
            return HasAnyUnit(source, effect);

        case EffectTargetType.AnyTarget:
            return HasAnyTarget(source, effect);

        default:
            return true;
    }
}
//////////////////
/// Return true if there are enemy units on field
/// //////////////


private bool HasEnemyUnit(
    RuntimeCard source,
    CardEffect effect)
{
    BattlefieldManager battlefield =
        GameManager.Instance.GetBattlefield(
            GetOpposingSide(source.Owner)
        );

    if (battlefield == null)
        return false;

    foreach (RuntimeCard minion in battlefield.Minions)
    {
        if (minion.IsStealthed)
            continue;

        if (effect.CanTarget(source, minion))
        {
            return true;
        }
    }

    return false;
}
//////////////////
/// Return if there are friendly units on field.
/// //////////////

private bool HasFriendlyUnit(
    RuntimeCard source,
    CardEffect effect)
{
    BattlefieldManager battlefield =
        GameManager.Instance.GetBattlefield(
            source.Owner
        );

    if (battlefield == null)
        return false;

    foreach (RuntimeCard minion in battlefield.Minions)
    {
        if (minion.IsStealthed)
            continue;

        if (effect.CanTarget(source, minion))
        {
            return true;
        }
    }

    return false;
}

//////////////////
/// Return if there is any unit on field.
/// //////////////

private bool HasAnyUnit(
    RuntimeCard source, CardEffect effect)
{
    BattlefieldManager playerBattlefield =
        GameManager.Instance.GetBattlefield(
            PlayerSide.Player
        );

    BattlefieldManager opponentBattlefield =
        GameManager.Instance.GetBattlefield(
            PlayerSide.Opponent
        );

    if (playerBattlefield != null)
    {
        foreach (RuntimeCard minion in playerBattlefield.Minions)
        {
            if (minion.IsStealthed)
                continue;

            if (effect.CanTarget(source, minion))
            {
                return true;
            }
        }
    }

    if (opponentBattlefield != null)
    {
        foreach (RuntimeCard minion in opponentBattlefield.Minions)
        {
            if (minion.IsStealthed)
                continue;

            if (effect.CanTarget(source, minion))
            {
                return true;
            }
        }
    }

    return false;
}

//////////////////
/// Opponent Side Player because we have 'sides'
/// //////////////


private PlayerSide GetOpposingSide(PlayerSide side)
{
    return side == PlayerSide.Player
        ? PlayerSide.Opponent
        : PlayerSide.Player;
}

//////////////////
/// Check if there are any targets including heroes and stuff. 
/// //////////////

private bool HasAnyTarget(
    RuntimeCard source, CardEffect effect)
{
    BattlefieldManager playerBattlefield =
        GameManager.Instance.GetBattlefield(
            PlayerSide.Player
        );

    BattlefieldManager opponentBattlefield =
        GameManager.Instance.GetBattlefield(
            PlayerSide.Opponent
        );

    if (effect.TargetFilter != EffectTargetFilter.None)
    {
        if (playerBattlefield != null)
        {
            foreach (RuntimeCard minion in playerBattlefield.Minions)
            {
                if (effect.CanTarget(source, minion))
                {
                    return true;
                }
            }
        }

        if (opponentBattlefield != null)
        {
            foreach (RuntimeCard minion in opponentBattlefield.Minions)
            {
                if (effect.CanTarget(source, minion))
                {
                    return true;
                }
            }
        }

        return false;
    }

    bool playerHasUnits =
        playerBattlefield != null &&
        playerBattlefield.Minions.Count > 0;

    bool opponentHasUnits =
        opponentBattlefield != null &&
        opponentBattlefield.Minions.Count > 0;

    bool playerHeroExists =
        GameManager.Instance.GetPlayerView(
            PlayerSide.Player
        ) != null;

    bool opponentHeroExists =
        GameManager.Instance.GetPlayerView(
            PlayerSide.Opponent
        ) != null;

    return playerHasUnits ||
           opponentHasUnits ||
           playerHeroExists ||
           opponentHeroExists;
}
}
