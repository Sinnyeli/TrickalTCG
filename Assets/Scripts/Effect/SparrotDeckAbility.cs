using System;
using UnityEngine;

// Scene component: deck abilities cannot be attached as battlefield passives.
public class SparrotDeckAbility : MonoBehaviour
{
    private static event Action<RuntimeCard> ApostlePlayed;
    private bool resolving;

    // Call exactly once after a successful Apostle play from hand/Commander zone.
    // Never call from BattlefieldManager.PlayCard (also used by generated summons).
    public static void NotifyApostlePlayed(RuntimeCard played)
    {
        if (played != null && played.Data is ApostleData) ApostlePlayed?.Invoke(played);
    }
    private void OnEnable() => ApostlePlayed += HandleApostlePlayed;
    private void OnDisable() => ApostlePlayed -= HandleApostlePlayed;

    private void HandleApostlePlayed(RuntimeCard played)
    {
        if (resolving || played.Data.CardID == "A_SPARROT" || GameManager.Instance == null) return;
        var game = GameManager.Instance;
        if (game.IsGameOver) return;
        var deck = game.GetDeck(played.Owner);
        var field = game.GetBattlefield(played.Owner);
        if (deck == null || field == null) return;
        resolving = true;
        try
        {
            // Only the copies present when this play is processed qualify.
            foreach (var card in deck.GetDrawPileSnapshot())
            {
                if (game.IsGameOver || !field.HasAvailableMinionSlot) break;
                if (card == null || card.IsCommander || !(card.Data is ApostleData) ||
                    card.Data.CardID != "A_SPARROT") continue;
                if (!deck.TryDrawRuntimeCard(card)) continue;
                if (!field.PlayCard(card)) { deck.ReturnDrawnCardToDeck(card); break; }
            }
        }
        finally { resolving = false; }
    }
}
