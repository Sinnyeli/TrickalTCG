using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JubeeAIController : MonoBehaviour
{
    [SerializeField] private HandManager handManager;
    [SerializeField] private BattlefieldManager battlefieldManager;
    [SerializeField] private BattlefieldManager enemyBattlefieldManager;
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private float actionDelay = .5f;
    private bool isTakingTurn;
    private void Update()
    {
        if (BattleSession.OpponentType == BattleOpponentType.RemotePlayer || isTakingTurn || GameManager.Instance == null || GameManager.Instance.IsGameOver) return;
        // Opponent-owned deathrattle choices can also occur during the player's turn.
        ResolveChoice();
    }
    private void Start()
    {
        if (BattleSession.OpponentType == BattleOpponentType.RemotePlayer) { enabled = false; return; }
        if (turnManager == null) turnManager = GameManager.Instance.TurnManager;
        if (handManager == null) handManager = GameManager.Instance.GetHandManager(PlayerSide.Opponent);
        if (battlefieldManager == null) battlefieldManager = GameManager.Instance.GetBattlefield(PlayerSide.Opponent);
        if (enemyBattlefieldManager == null) enemyBattlefieldManager = GameManager.Instance.GetBattlefield(PlayerSide.Player);
        turnManager.OnTurnStarted += HandleTurnStarted;
        if (turnManager.IsMyTurn(PlayerSide.Opponent)) TakeTurn();
    }
    public void TakeTurn()
    {
        if (!isTakingTurn && BattleSession.OpponentType != BattleOpponentType.RemotePlayer && turnManager != null && turnManager.IsMyTurn(PlayerSide.Opponent)) StartCoroutine(TakeTurnRoutine());
    }
    private bool Active => GameManager.Instance != null && !GameManager.Instance.IsGameOver && turnManager.IsMyTurn(PlayerSide.Opponent);
    private IEnumerator TakeTurnRoutine()
    {
        isTakingTurn = true;
        yield return new WaitForSeconds(actionDelay);
        int actions = 0;
        while (Active && actions++ < 100)
        {
            if (ResolveChoice()) { yield return new WaitForSeconds(actionDelay); continue; }
            bool played = false;
            foreach (var card in new List<RuntimeCard>(handManager.Hand))
            {
                if (!Active) break;
                if (card.GetManaCost() > turnManager.OpponentMana) continue;
                if (TryPlay(card)) { played = true; break; }
            }
            if (!played) break;
            yield return new WaitForSeconds(actionDelay);
        }
        foreach (var attacker in new List<RuntimeCard>(battlefieldManager.Minions))
        {
            // Multiple attacks granted by effects use the same requests and validations.
            for (int attempts = 0; Active && attacker.Zone == CardZone.Field && attacker.CanAttack && attempts < 12; attempts++)
            {
                bool attacked = false;
                var targets = new List<RuntimeCard>(enemyBattlefieldManager.Minions);
                targets.Sort((a,b) => b.HasKeyword(CardKeyword.Taunt).CompareTo(a.HasKeyword(CardKeyword.Taunt)));
                foreach (var target in targets)
                    if (!target.IsStealthed && BattleActions.Submit(PlayerSide.Opponent, BattleActionKind.AttackUnit, attacker, target)) { attacked = true; break; }
                if (!attacked) attacked = BattleActions.Submit(PlayerSide.Opponent, BattleActionKind.AttackHero, attacker, hero: PlayerSide.Player);
                if (!attacked) break;
                yield return new WaitForSeconds(actionDelay);
                while (Active && ResolveChoice()) yield return new WaitForSeconds(actionDelay);
            }
        }
        while (Active && ResolveChoice()) yield return new WaitForSeconds(actionDelay);
        yield return new WaitForSeconds(actionDelay);
        if (Active) BattleActions.Submit(PlayerSide.Opponent, BattleActionKind.EndTurn);
        isTakingTurn = false;
    }
    private bool TryPlay(RuntimeCard card)
    {
        if (card.Data is ArtifactData)
        {
            foreach (var unit in battlefieldManager.Minions)
                if (BattleActions.Submit(PlayerSide.Opponent, BattleActionKind.EquipArtifact, card, unit)) return true;
            return false;
        }
        if (card.Data is SpellData spell)
        {
            CardEffect manual = null;
            foreach (var effect in spell.SpellEffects)
                if (effect != null && (effect.TargetType == EffectTargetType.FriendlyUnit || effect.TargetType == EffectTargetType.EnemyUnit ||
                    effect.TargetType == EffectTargetType.AnyUnit || effect.TargetType == EffectTargetType.AnyTarget || effect.TargetType == EffectTargetType.EnemyTarget ||
                    effect.TargetType == EffectTargetType.FriendlyHero || effect.TargetType == EffectTargetType.EnemyHero)) { manual = effect; break; }
            if (manual != null)
            {
                foreach (PlayerSide side in new[] { PlayerSide.Opponent, PlayerSide.Player })
                    foreach (var target in GameManager.Instance.GetBattlefield(side).Minions)
                        if (EffectTargetManager.Instance.CanChooseFor(card, manual, target) && BattleActions.Submit(PlayerSide.Opponent, BattleActionKind.PlaySpellOnUnit, card, target)) return true;
                foreach (PlayerSide side in new[] { PlayerSide.Opponent, PlayerSide.Player })
                    if (EffectTargetManager.Instance.CanChooseHeroFor(card, manual, GameManager.Instance.GetPlayerView(side)) &&
                        BattleActions.Submit(PlayerSide.Opponent, BattleActionKind.PlaySpellOnHero, card, hero: side)) return true;
                return false;
            }
        }
        return BattleActions.Submit(PlayerSide.Opponent, BattleActionKind.PlayCard, card);
    }
    private bool ResolveChoice()
    {
        var choice = BattleChoiceRequests.Pending;
        if (choice != null && choice.owner == PlayerSide.Opponent && choice.cards.Count > 0)
            return BattleActions.Submit(PlayerSide.Opponent, BattleActionKind.ChooseCard, target: choice.cards[Random.Range(0, choice.cards.Count)], choice: choice.id);
        var targeting = EffectTargetManager.Instance;
        if (targeting == null || !targeting.IsSelectingTarget || targeting.PendingOwner != PlayerSide.Opponent) return false;
        foreach (PlayerSide side in new[] { PlayerSide.Player, PlayerSide.Opponent })
            foreach (var unit in GameManager.Instance.GetBattlefield(side).Minions)
                if (targeting.CanChoose(unit)) return BattleActions.Submit(PlayerSide.Opponent, BattleActionKind.ChooseUnit, target: unit);
        foreach (PlayerSide side in new[] { PlayerSide.Player, PlayerSide.Opponent })
            if (targeting.CanChooseHero(GameManager.Instance.GetPlayerView(side))) return BattleActions.Submit(PlayerSide.Opponent, BattleActionKind.ChooseHero, hero: side);
        targeting.CancelTargetSelection(); return false;
    }
    private void HandleTurnStarted(PlayerSide side)
    {
        if (side != PlayerSide.Opponent) { StopAllCoroutines(); isTakingTurn = false; return; }
        TakeTurn();
    }
    private void OnDestroy() { if (turnManager != null) turnManager.OnTurnStarted -= HandleTurnStarted; }
}
