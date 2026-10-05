using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RemoteCardState
{
    public string id, dataID;
    public bool commander, ready, frozen, silenced, cannotHero;
    public int cost, attack, health, maxHealth;
    public CardZone zone;
    public PlayerSide owner;
    public List<CardKeyword> keywords = new List<CardKeyword>();
    public List<RemoteCardState> artifacts = new List<RemoteCardState>();
    public static RemoteCardState Capture(RuntimeCard card)
    {
        var value = new RemoteCardState { id = card.InstanceID, dataID = card.Data.CardID, commander = card.IsCommander,
            ready = card.CanAttack, frozen = card.IsFrozen, silenced = card.IsSilenced, cannotHero = card.CannotAttackHero,
            cost = card.GetManaCost(), attack = card.GetAttack(), health = card.CurrentHealth, maxHealth = card.GetMaxHealth(), zone = card.Zone, owner = card.Owner };
        foreach (CardKeyword key in Enum.GetValues(typeof(CardKeyword))) if (card.HasKeyword(key)) value.keywords.Add(key);
        // Only three equipment portraits are rendered; aggregate stats and keywords cover all inherited equipment.
        for (int i = 0; i < Math.Min(3, card.EquippedArtifacts.Count); i++) value.artifacts.Add(Capture(card.EquippedArtifacts[i]));
        return value;
    }
}
[Serializable]
public class RemoteBattleState
{
    public string matchID;
    public int turn;
    public PlayerSide active;
    public int ownMana, ownMaxMana, enemyMana, enemyMaxMana, ownHealth, enemyHealth, ownDeck, enemyDeck, enemyHand;
    public float seconds;
    public bool over;
    public PlayerSide winner;
    public List<RemoteCardState> hand = new List<RemoteCardState>();
    public List<RemoteCardState> ownField = new List<RemoteCardState>(), enemyField = new List<RemoteCardState>();
    public RemoteCardState enemyCommander;
    public bool targetPrompt;
    public List<string> targetUnits = new List<string>();
    public List<PlayerSide> targetHeroes = new List<PlayerSide>();
    public string choiceID;
    public List<RemoteCardState> choices = new List<RemoteCardState>();
    public static RemoteBattleState Capture()
    {
        var game = GameManager.Instance; var turn = game.TurnManager;
        // Payload is projected for the guest only: no host hand identities or draw-pile order.
        var state = new RemoteBattleState { matchID = BattleActions.MatchID, turn = turn.TurnNumber, active = Swap(turn.CurrentSide),
            ownMana = turn.OpponentMana, ownMaxMana = turn.OpponentMaxMana, enemyMana = turn.PlayerMana, enemyMaxMana = turn.PlayerMaxMana,
            ownHealth = game.GetPlayerView(PlayerSide.Opponent).CurrentHealth, enemyHealth = game.GetPlayerView(PlayerSide.Player).CurrentHealth,
            ownDeck = game.GetDeck(PlayerSide.Opponent).RemainingCards, enemyDeck = game.GetDeck(PlayerSide.Player).RemainingCards,
            enemyHand = game.GetHandManager(PlayerSide.Player).HandSize, seconds = turn.RemainingTurnSeconds,
            over = game.IsGameOver, winner = game.WinnerSide == PlayerSide.Player ? PlayerSide.Opponent : PlayerSide.Player };
        foreach (var card in game.GetHandManager(PlayerSide.Opponent).Hand) state.hand.Add(RemoteCardState.Capture(card));
        foreach (var card in game.GetHandManager(PlayerSide.Player).Hand) if (card.IsCommander) state.enemyCommander = RemoteCardState.Capture(card);
        foreach (var card in game.GetBattlefield(PlayerSide.Opponent).Minions) state.ownField.Add(RemoteCardState.Capture(card));
        foreach (var card in game.GetBattlefield(PlayerSide.Player).Minions) state.enemyField.Add(RemoteCardState.Capture(card));
        var target = EffectTargetManager.Instance;
        if (target != null && target.HasPendingTarget && target.PendingOwner == PlayerSide.Opponent)
        {
            state.targetPrompt = true;
            foreach (PlayerSide side in new[] { PlayerSide.Player, PlayerSide.Opponent })
            {
                foreach (var card in game.GetBattlefield(side).Minions) if (target.CanChoose(card)) state.targetUnits.Add(card.InstanceID);
                if (target.CanChooseHero(game.GetPlayerView(side))) state.targetHeroes.Add(Swap(side));
            }
        }
        var choice = BattleChoiceRequests.Pending;
        if (choice != null && choice.owner == PlayerSide.Opponent)
        { state.choiceID = choice.id; foreach (var card in choice.cards) { var option = RemoteCardState.Capture(card); option.artifacts.Clear(); state.choices.Add(option); } }
        return state;
    }
    public static PlayerSide Swap(PlayerSide side) => side == PlayerSide.Player ? PlayerSide.Opponent : PlayerSide.Player;
}
