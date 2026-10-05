using System;
using System.Collections.Generic;
using UnityEngine;

// All player inputs and AI decisions enter here. Internal effect resolution stays synchronous.
public static class BattleActions
{
    public static bool Executing { get; private set; }
    private static readonly Dictionary<string, BattleActionResult> results = new Dictionary<string, BattleActionResult>();
    private static readonly Queue<string> resultOrder = new Queue<string>();
    private static string resultMatch;
    public static string MatchID { get; private set; } = Guid.NewGuid().ToString("N");
    public static void ResetMatch(string id = null)
    {
        MatchID = id ?? Guid.NewGuid().ToString("N"); results.Clear(); resultOrder.Clear(); resultMatch = MatchID;
        BattleChoiceRequests.Clear();
    }
    public static bool Submit(PlayerSide side, BattleActionKind kind, RuntimeCard card = null, RuntimeCard target = null,
        PlayerSide hero = PlayerSide.Opponent, string choice = null)
    {
        var game = GameManager.Instance;
        if (game == null) return false;
        var request = new BattleActionRequest { requestID = Guid.NewGuid().ToString("N"), matchID = MatchID,
            turn = game.TurnManager.TurnNumber, kind = kind, cardID = card?.InstanceID,
            targetID = target?.InstanceID, hero = hero, choiceID = choice };
        if (UnityRemoteMatch.IsGuest)
        {
            if (side != PlayerSide.Player) return false;
            // Visual sides are reversed on the guest; transport sends canonical host seats.
            request.hero = hero == PlayerSide.Player ? PlayerSide.Opponent : PlayerSide.Player;
            return UnityRemoteMatch.Instance.SendAction(request);
        }
        var result = Execute(side, request);
        if (!result.accepted && side == PlayerSide.Player &&
            (!(kind == BattleActionKind.AttackUnit || kind == BattleActionKind.AttackHero) || result.error != "Action is not legal."))
            CombatFailureUI.ShowText(result.error);
        return result.accepted;
    }
    // The transport supplies the authenticated seat; a request cannot choose its own owner.
    public static BattleActionResult Execute(PlayerSide seat, BattleActionRequest request)
    {
        var result = new BattleActionResult { requestID = request?.requestID };
        if (request == null || string.IsNullOrEmpty(request.requestID) || request.requestID.Length > 64)
        { result.error = "Invalid request."; return result; }
        if (resultMatch != MatchID) { results.Clear(); resultOrder.Clear(); resultMatch = MatchID; }
        string key = seat + ":" + request.requestID;
        if (results.TryGetValue(key, out var previous)) return previous;
        try
        {
            var game = GameManager.Instance;
            if (Executing || game == null || game.IsGameOver || UnityRemoteMatch.IsGuest) return Reject(result, "Match is not accepting actions.");
            if (request.matchID != MatchID) return Reject(result, "Wrong match.");
            bool isChoice = request.kind == BattleActionKind.ChooseCard || request.kind == BattleActionKind.ChooseUnit || request.kind == BattleActionKind.ChooseHero;
            if (request.kind != BattleActionKind.Surrender &&
                (request.turn != game.TurnManager.TurnNumber || (!isChoice && !game.TurnManager.IsMyTurn(seat))))
                return Reject(result, "It is not your turn, or this request has expired.");
            bool pending = BattleChoiceRequests.HasPending || (EffectTargetManager.Instance != null && EffectTargetManager.Instance.HasPendingTarget);
            if (pending && request.kind != BattleActionKind.ChooseCard && request.kind != BattleActionKind.ChooseUnit &&
                request.kind != BattleActionKind.ChooseHero && request.kind != BattleActionKind.Surrender)
                return Reject(result, "Complete the pending choice first.");
            RuntimeCard card = Find(request.cardID), target = Find(request.targetID);
            Executing = true;
            switch (request.kind)
            {
                case BattleActionKind.PlayCard:
                    if (!InHand(card, seat)) break;
                    result.accepted = card.IsCommander ? game.GetHandManager(seat).PlayCommanderFromHand(card) : game.GetHandManager(seat).PlayCardFromHand(card); break;
                case BattleActionKind.PlaySpellOnUnit:
                    if (InHand(card, seat) && target != null && target.Zone == CardZone.Field && target.CurrentHealth > 0 &&
                        game.GetBattlefield(target.Owner).Minions.ContainsCard(target)) result.accepted = game.GetHandManager(seat).PlayTargetedSpellFromHand(card, target); break;
                case BattleActionKind.PlaySpellOnHero:
                    if (InHand(card, seat)) result.accepted = game.GetHandManager(seat).PlayTargetedSpellFromHand(card, game.GetPlayerView(request.hero)); break;
                case BattleActionKind.EquipArtifact:
                    result.accepted = Equip(seat, card, target); break;
                case BattleActionKind.AttackUnit:
                case BattleActionKind.AttackHero:
                    if (card == null || card.Owner != seat || card.Zone != CardZone.Field || !game.GetBattlefield(seat).Minions.ContainsCard(card)) break;
                    if (!game.CombatManager.SelectAttacker(card)) break;
                    result.accepted = request.kind == BattleActionKind.AttackUnit ? game.CombatManager.Attack(target) : game.CombatManager.Attack(game.GetPlayerView(request.hero));
                    game.CombatManager.ClearSelection(); break;
                case BattleActionKind.EndTurn: game.TurnManager.EndTurn(); result.accepted = true; break;
                case BattleActionKind.Surrender: game.PlayerDefeated(seat); result.accepted = true; break;
                case BattleActionKind.ChooseCard: result.accepted = BattleChoiceRequests.Resolve(seat, request.choiceID, request.targetID); break;
                case BattleActionKind.ChooseUnit:
                    if (EffectTargetManager.Instance != null && EffectTargetManager.Instance.PendingOwner == seat && EffectTargetManager.Instance.CanChoose(target))
                    { EffectTargetManager.Instance.SelectTarget(target); result.accepted = true; } break;
                case BattleActionKind.ChooseHero:
                    if (EffectTargetManager.Instance != null && EffectTargetManager.Instance.PendingOwner == seat && EffectTargetManager.Instance.CanChooseHero(game.GetPlayerView(request.hero)))
                    { EffectTargetManager.Instance.SelectHeroTarget(game.GetPlayerView(request.hero)); result.accepted = true; } break;
            }
            if (!result.accepted && result.error == null) result.error = "Action is not legal.";
            return result;
        }
        catch (Exception error) { Debug.LogException(error); result.error = "Action failed; see host diagnostics."; return result; }
        finally
        {
            Executing = false;
            UnityRemoteMatch.Instance?.AuthorityActionResolved(request, result);
            results[key] = result; resultOrder.Enqueue(key);
            while (resultOrder.Count > 256) results.Remove(resultOrder.Dequeue());
        }
    }
    private static BattleActionResult Reject(BattleActionResult result, string message) { result.error = message; return result; }
    private static bool InHand(RuntimeCard card, PlayerSide seat) => card != null && card.Owner == seat &&
        card.Zone == CardZone.Hand && GameManager.Instance.GetHandManager(seat).Hand.ContainsCard(card);
    public static RuntimeCard Find(string id)
    {
        if (string.IsNullOrEmpty(id) || GameManager.Instance == null) return null;
        foreach (PlayerSide side in new[] { PlayerSide.Player, PlayerSide.Opponent })
        {
            foreach (var card in GameManager.Instance.GetHandManager(side).Hand) if (card.InstanceID == id) return card;
            foreach (var card in GameManager.Instance.GetBattlefield(side).Minions) if (card.InstanceID == id) return card;
        }
        return null;
    }
    private static bool Equip(PlayerSide seat, RuntimeCard artifact, RuntimeCard target)
    {
        var game = GameManager.Instance;
        if (!InHand(artifact, seat) || !(artifact.Data is ArtifactData) || target == null || !(target.Data is ApostleData) ||
            target.Owner != seat || target.Zone != CardZone.Field || !game.GetBattlefield(seat).Minions.ContainsCard(target) ||
            target.EquippedArtifacts.Count >= target.GetMaxEquipment()) return false;
        int cost = artifact.GetManaCost();
        if (!game.TurnManager.CanSpendMana(seat, cost)) return false;
        if (!target.EquipArtifact(artifact)) return false;
        game.ConsumeNextCardDiscount(seat); game.TurnManager.SpendMana(seat, cost);
        game.GetHandManager(seat).RemoveCardFromHand(artifact); game.GetBattlefield(seat).RefreshMinionView(target);
        return true;
    }
}
public static class BattleCardListExtensions
{
    public static bool ContainsCard(this IReadOnlyList<RuntimeCard> cards, RuntimeCard card)
    { for (int i = 0; i < cards.Count; i++) if (cards[i] == card) return true; return false; }
}
