using System;
using System.Collections.Generic;
using UnityEngine;

public static class BattleChoiceRequests
{
    public sealed class Choice
    {
        public string id;
        public PlayerSide owner;
        public List<RuntimeCard> cards;
        public Action<RuntimeCard> callback;
    }
    private static readonly Queue<Choice> choices = new Queue<Choice>();
    private static Choice pending;
    public static bool HasPending => pending != null;
    public static bool IsPendingOwner(PlayerSide side) => pending != null && pending.owner == side;
    public static Choice Pending => pending;
    public static string Register(PlayerSide owner, List<RuntimeCard> cards, Action<RuntimeCard> callback)
    {
        var choice = new Choice { id = Guid.NewGuid().ToString("N"), owner = owner, cards = new List<RuntimeCard>(cards), callback = callback };
        if (pending == null) pending = choice; else choices.Enqueue(choice);
        return choice.id;
    }
    public static bool Resolve(PlayerSide owner, string id, string cardID)
    {
        if (pending == null || pending.owner != owner || pending.id != id) return false;
        var card = pending.cards.Find(c => c.InstanceID == cardID);
        if (card == null) return false;
        var callback = pending.callback; pending = choices.Count > 0 ? choices.Dequeue() : null; callback(card); return true;
    }
    public static void Expire(PlayerSide side) { if (pending != null && pending.owner == side) Clear(); }
    public static void Clear() { pending = null; choices.Clear(); }
}
