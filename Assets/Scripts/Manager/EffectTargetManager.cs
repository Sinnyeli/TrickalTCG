using System.Collections.Generic;
using UnityEngine;

public class EffectTargetManager : MonoBehaviour
{
    public static EffectTargetManager Instance { get; private set; }

    private RuntimeCard sourceCard;
    private CardEffect currentEffect;

    public bool IsSelectingTarget =>
        currentEffect != null;

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
        CardEffect effect)
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
        if (!IsSelectingTarget)
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
    if (!IsSelectingTarget)
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

    ClearTargetSelection();
}



//////////////////
/// Check if the target selected is valid
/// //////////////

    private bool IsValidTarget(RuntimeCard target)
    {
        if (sourceCard == null || currentEffect == null)
            return false;

        switch (currentEffect.TargetType)
        {
            case EffectTargetType.EnemyUnit:
                return target.Owner != sourceCard.Owner;

            case EffectTargetType.FriendlyUnit:
                return target.Owner == sourceCard.Owner;

            case EffectTargetType.AnyUnit:
                return true;
            case EffectTargetType.AnyTarget:
                return true;

            default:
                return false;
        }
    }

        private bool IsValidHeroTarget(PlayerView target)
    {
        if (sourceCard == null || currentEffect == null)
            return false;

        switch (currentEffect.TargetType)
        {
            case EffectTargetType.EnemyHero:
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
        case EffectTargetType.EnemyUnit:
            return HasEnemyUnit(source);

        case EffectTargetType.FriendlyUnit:
            return HasFriendlyUnit(source);

        case EffectTargetType.AnyUnit:
            return HasAnyUnit();

        case EffectTargetType.AnyTarget:
        return HasAnyTarget();

        default:
            return true;
    }
}

//////////////////
/// Return true if there are enemy units on field
/// //////////////


private bool HasEnemyUnit(RuntimeCard source)
{
    BattlefieldManager battlefield =
        GameManager.Instance.GetBattlefield(
            GetOpposingSide(source.Owner)
        );

    return battlefield != null &&
           battlefield.Minions.Count > 0;
}

//////////////////
/// Return if there are friendly units on field.
/// //////////////

private bool HasFriendlyUnit(RuntimeCard source)
{
    BattlefieldManager battlefield =
        GameManager.Instance.GetBattlefield(
            source.Owner
        );

    return battlefield != null &&
           battlefield.Minions.Count > 0;
}

//////////////////
/// Return if there is any unit on field.
/// //////////////

private bool HasAnyUnit()
{
    BattlefieldManager playerBattlefield =
        GameManager.Instance.GetBattlefield(
            PlayerSide.Player
        );

    BattlefieldManager opponentBattlefield =
        GameManager.Instance.GetBattlefield(
            PlayerSide.Opponent
        );

    bool playerHasUnits =
        playerBattlefield != null &&
        playerBattlefield.Minions.Count > 0;

    bool opponentHasUnits =
        opponentBattlefield != null &&
        opponentBattlefield.Minions.Count > 0;

    return playerHasUnits || opponentHasUnits;
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
/// Check if there are any targets without entering cards source or etc. 
/// //////////////

private bool HasAnyTarget()
{
    BattlefieldManager playerBattlefield =
        GameManager.Instance.GetBattlefield(PlayerSide.Player);

    BattlefieldManager opponentBattlefield =
        GameManager.Instance.GetBattlefield(PlayerSide.Opponent);

    bool playerHasUnits =
        playerBattlefield != null &&
        playerBattlefield.Minions.Count > 0;

    bool opponentHasUnits =
        opponentBattlefield != null &&
        opponentBattlefield.Minions.Count > 0;

    bool playerHeroExists =
        GameManager.Instance.GetPlayerView(PlayerSide.Player) != null;

    bool opponentHeroExists =
        GameManager.Instance.GetPlayerView(PlayerSide.Opponent) != null;

    return playerHasUnits ||
           opponentHasUnits ||
           playerHeroExists ||
           opponentHeroExists;
}

}