#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Editor-only harness: gameplay remains in the production managers/action gateway.
[DefaultExecutionOrder(-10000)]
public sealed class CardTestSandbox : MonoBehaviour
{
    public CardDatabase database;
    private GameManager game;
    private PlayerSide side;
    private RuntimeCard selectedUnit, target, handCard;
    private bool ready;
    private CardData catalogCard;
    private string search = "", amount = "1", attack = "2", health = "2", mana = "10", maxMana = "10", heroHealth = "20";
    private bool show = true, battlecry = true, commander;
    private Vector2 scroll, catalogScroll, logScroll;
    private int keyword;
    private readonly List<string> logs = new List<string>();
    private GameObject blocker;
    private string failure;
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private void Awake()
    {
        if (UnityRemoteMatch.Instance != null)
        {
            failure = "Stop Play Mode and open CardTest directly. A network session is still loaded.";
            enabled = false;
            Debug.LogError(failure);
            return;
        }
        BattleSession.Clear();
        foreach (var loader in FindObjectsOfType<BattleDeckLoader>(true)) loader.enabled = false;
        foreach (var setup in FindObjectsOfType<MatchSetup>(true)) setup.enabled = false;
        foreach (var ai in FindObjectsOfType<JubeeAIController>(true)) ai.enabled = false;
        foreach (var turns in FindObjectsOfType<TurnManager>(true)) turns.enabled = false;
        Application.logMessageReceived += Log;
    }
    private IEnumerator Start()
    {
        yield return null;
        Run(() => {
            game = GameManager.Instance;
            if (game == null || database == null) throw new InvalidOperationException("Sandbox requires GameManager and CardDatabase.");
            game.PrepareRemotePresentation();
            game.SetGameState(GameState.Playing);
            var defaultCommander = database.AllCards.OfType<ApostleData>().FirstOrDefault(c => c.Collectible);
            if (defaultCommander == null) throw new InvalidOperationException("No Apostle found in database.");
            foreach (PlayerSide seat in Enum.GetValues(typeof(PlayerSide)))
            {
                game.GetDeck(seat).InitializeDeck(defaultCommander, new List<CardData>());
                // No Commander in hand by default; add a chosen Apostle using the Commander toggle.
            }
            BattleActions.ResetMatch();
            game.TurnManager.Initialize();
            SetMana(PlayerSide.Player, 10, 10); SetMana(PlayerSide.Opponent, 10, 10);
            MakeBlocker();
            ready = true;
            Log("Sandbox ready. AI/timer disabled; normal mana, targeting and field limits still apply.", "", LogType.Log);
        });
    }
    private void MakeBlocker()
    {
        blocker = new GameObject("Sandbox input shield", typeof(Canvas), typeof(GraphicRaycaster));
        var canvas = blocker.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 32000;
        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(blocker.transform, false);
        var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 0); rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, .5f); rect.sizeDelta = new Vector2(440, 0); rect.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0, 0, 0, 0);
    }
    private void OnDestroy()
    {
        Application.logMessageReceived -= Log;
        if (blocker != null) Destroy(blocker);
    }
    private void Log(string message, string stack, LogType type)
    {
        logs.Add($"[{Time.time:F1}] {type}: {message}");
        if (type == LogType.Exception) logs.Add(stack);
        while (logs.Count > 160) logs.RemoveAt(0);
        logScroll.y = float.MaxValue;
    }
    private void Run(Action action)
    {
        try { action(); } catch (Exception e) { failure = e.Message; Debug.LogException(e); }
    }
    private static void SetField(object instance, string name, object value)
    {
        var field = instance.GetType().GetField(name, Private);
        if (field == null) throw new MissingFieldException(instance.GetType().Name, name);
        field.SetValue(instance, value);
    }
    private void SetMana(PlayerSide seat, int current, int maximum)
    {
        string prefix = seat == PlayerSide.Player ? "player" : "opponent";
        maximum = Mathf.Clamp(maximum, 0, 10);
        SetField(game.TurnManager, prefix + "MaxMana", maximum);
        SetField(game.TurnManager, prefix + "Mana", Mathf.Clamp(current, 0, maximum));
    }
    private int Number(string value, int fallback = 1) => int.TryParse(value, out var n) ? n : fallback;
    private void Submit(BattleActionKind kind, RuntimeCard card = null, RuntimeCard unit = null, PlayerSide hero = PlayerSide.Player)
    {
        if (!BattleActions.Submit(side, kind, card, unit, hero)) Debug.LogWarning("Sandbox action rejected. Check turn, mana, targets and pending choices.");
    }
    private void AddHand()
    {
        if (catalogCard == null) return;
        var card = new RuntimeCard(catalogCard, commander && catalogCard is ApostleData);
        card.SetOwner(side); card.ChangeZone(CardZone.Hand);
        if (game.GetHandManager(side).AddCard(card)) handCard = card;
    }
    private void Summon()
    {
        if (!(catalogCard is MinionData)) { Debug.LogWarning("Select a minion to summon."); return; }
        var card = new RuntimeCard(catalogCard, commander && catalogCard is ApostleData);
        card.SetOwner(side); card.ChangeZone(CardZone.Hand);
        // Suppress only this source's battlecry. Other entry reactions still run normally.
        SetField(card, "<SuppressSummonBattlecry>k__BackingField", !battlecry);
        try { if (game.GetBattlefield(side).PlayCard(card)) selectedUnit = card; }
        finally { SetField(card, "<SuppressSummonBattlecry>k__BackingField", false); }
    }
    private void AddDeck(bool top)
    {
        if (catalogCard == null) return;
        var deck = game.GetDeck(side);
        deck.AddCardToDeck(catalogCard, false);
        if (top)
        {
            // drawPile[0] is the next draw. Preserve the identities/order of all existing cards.
            var pile = (List<RuntimeCard>)typeof(DeckManager).GetField("drawPile", Private).GetValue(deck);
            var added = pile[pile.Count - 1]; pile.RemoveAt(pile.Count - 1); pile.Insert(0, added);
        }
    }
    private void RefreshUnit()
    {
        if (selectedUnit == null) return;
        game.CombatManager.CheckDeath(selectedUnit);
        foreach (PlayerSide seat in Enum.GetValues(typeof(PlayerSide)))
        {
            var field = game.GetBattlefield(seat); field.RefreshPassives();
            foreach (var card in field.Minions.ToArray()) field.RefreshMinionView(card);
        }
    }
    private void ClearZone(bool field)
    {
        if (field)
        {
            var board = game.GetBattlefield(side);
            // Setup clear deliberately skips deaths and leave triggers. Use Kill for rule testing.
            var removed = board.Minions.ToArray();
            board.ApplyRemoteField(new List<RuntimeCard>());
            foreach (var card in removed)
            {
                foreach (var artifact in card.EquippedArtifacts)
                    foreach (PlayerSide seat in Enum.GetValues(typeof(PlayerSide)))
                        foreach (var survivor in game.GetBattlefield(seat).Minions)
                            survivor.RemoveModifiersFromSource(artifact);
                card.ChangeZone(CardZone.Graveyard);
            }
            board.RefreshPassives(); board.RefreshArtifactEffects();
        }
        else
        {
            var hand = game.GetHandManager(side);
            foreach (var card in hand.Hand.ToArray()) { hand.RemoveCardFromHand(card); card.ChangeZone(CardZone.Graveyard); }
        }
        selectedUnit = target = handCard = null;
    }
    private void Button(string text, Action action)
    {
        if (GUILayout.Button(text)) Run(action);
    }
    private void Heading(string text) { GUILayout.Space(9); GUILayout.Label(text, UnityEditor.EditorStyles.boldLabel); }
    private string Input(string label, string value)
    {
        GUILayout.BeginHorizontal(); GUILayout.Label(label, GUILayout.Width(145)); value = GUILayout.TextField(value); GUILayout.EndHorizontal(); return value;
    }
    private string Label(RuntimeCard card) => card == null ? "none" : $"{card.Data.cardName} [{card.InstanceID.Substring(0, 5)}] {card.GetAttack()}/{card.CurrentHealth}";
    private void OnGUI()
    {
        GUI.depth = -100;
        if (!show)
        {
            if (GUI.Button(new Rect(8, 8, 160, 32), "Show Card Test")) { show = true; if (blocker != null) blocker.SetActive(true); }
            return;
        }
        GUILayout.BeginArea(new Rect(8, 8, 424, Screen.height - 16), GUI.skin.box);
        GUILayout.Label("CARD TEST — local / Editor only");
        if (GUILayout.Button("Hide panel (restore board access)")) { show = false; if (blocker != null) blocker.SetActive(false); }
        if (!ready) { GUILayout.Label(failure ?? "Starting sandbox…"); GUILayout.EndArea(); return; }
        scroll = GUILayout.BeginScrollView(scroll);
        GUILayout.Label($"Turn {game.TurnManager.TurnNumber}: {game.TurnManager.CurrentSide} | {game.CurrentGameState}");
        side = (PlayerSide)GUILayout.Toolbar((int)side, new[] { "Player controls", "Opponent controls" });
        GUILayout.Label("Selection chooses setup/action owner; it does not change the active turn.");
        Button("End active turn (resolve all turn effects)", () => BattleActions.Submit(game.TurnManager.CurrentSide, BattleActionKind.EndTurn));
        Button("Reset entire test", () => UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(SceneManager.GetActiveScene().path, new LoadSceneParameters(LoadSceneMode.Single)));
        Heading("Find any card (including generated cards)");
        search = Input("Name / Card ID", search);
        catalogScroll = GUILayout.BeginScrollView(catalogScroll, GUILayout.Height(145));
        foreach (var card in database.AllCards.Where(c => c != null && (string.IsNullOrWhiteSpace(search) ||
            (c.cardName ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
            (c.CardID ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0)))
            if (GUILayout.Button($"{card.cardName} | {card.CardID} | {card.manaCost} mana")) catalogCard = card;
        GUILayout.EndScrollView();
        GUILayout.Label("Selected: " + (catalogCard == null ? "none" : catalogCard.CardID));
        if (catalogCard != null) GUILayout.Label(catalogCard.description);
        commander = GUILayout.Toggle(commander, "Create as Commander (Apostles only)");
        battlecry = GUILayout.Toggle(battlecry, "Trigger Battlecry on setup summon");
        Button("Add selected card to hand", AddHand);
        Button("Summon selected minion (no mana cost)", Summon);
        GUILayout.BeginHorizontal(); Button("Add to deck bottom", () => AddDeck(false)); Button("Add as next draw", () => AddDeck(true)); GUILayout.EndHorizontal();
        Button("Draw normally (runs on-draw effects)", () => game.DrawCard(side));
        Button("Empty draw pile (setup only)", () => {
            var pile = (List<RuntimeCard>)typeof(DeckManager).GetField("drawPile", Private).GetValue(game.GetDeck(side));
            pile.Clear();
        });
        GUILayout.Label($"Draw pile: {game.GetDeck(side).RemainingCards} | Hand: {game.GetHandManager(side).HandSize}");
        GUILayout.Label("Next draws: " + string.Join(" → ", game.GetDeck(side).GetDrawPileSnapshot().Take(8).Select(c => c.Data.cardName)));
        Heading("Player state — selected side");
        mana = Input("Available mana", mana); maxMana = Input("Unlocked mana", maxMana);
        Button("Apply mana", () => SetMana(side, Number(mana, 10), Number(maxMana, 10)));
        heroHealth = Input("Player health (0–20)", heroHealth);
        Button("Set player health (setup)", () => game.GetPlayerView(side).ApplyRemoteHealth(Mathf.Clamp(Number(heroHealth, 20), 0, 20)));
        Button("Damage player (normal rules)", () => game.GetPlayerView(side).TakeDamage(Mathf.Max(0, Number(amount))));
        Heading("Hand — select a card, then play/cast/equip");
        foreach (var card in game.GetHandManager(side).Hand.ToArray())
            if (GUILayout.Button((card == handCard ? "● " : "") + Label(card))) handCard = card;
        GUILayout.Label("Hand selection: " + Label(handCard));
        Button("Play selected card normally", () => Submit(BattleActionKind.PlayCard, handCard));
        Button("Cast spell on target unit", () => Submit(BattleActionKind.PlaySpellOnUnit, handCard, target));
        GUILayout.BeginHorizontal();
        Button("Cast on Player", () => Submit(BattleActionKind.PlaySpellOnHero, handCard, hero: PlayerSide.Player));
        Button("Cast on Opponent", () => Submit(BattleActionKind.PlaySpellOnHero, handCard, hero: PlayerSide.Opponent));
        GUILayout.EndHorizontal();
        Button("Equip artifact onto target unit", () => Submit(BattleActionKind.EquipArtifact, handCard, target));
        Heading("Units — choose actor and target independently");
        foreach (PlayerSide seat in Enum.GetValues(typeof(PlayerSide)))
        {
            GUILayout.Label(seat.ToString());
            foreach (var card in game.GetBattlefield(seat).Minions.ToArray())
            {
                GUILayout.BeginHorizontal(); GUILayout.Label(Label(card));
                if (GUILayout.Button("Actor", GUILayout.Width(52))) selectedUnit = card;
                if (GUILayout.Button("Target", GUILayout.Width(54))) target = card;
                GUILayout.EndHorizontal();
            }
        }
        GUILayout.Label("Actor: " + Label(selectedUnit)); GUILayout.Label("Target: " + Label(target));
        Button("Actor attacks target (normal rules)", () => Submit(BattleActionKind.AttackUnit, selectedUnit, target));
        GUILayout.BeginHorizontal();
        Button("Attack Player", () => Submit(BattleActionKind.AttackHero, selectedUnit, hero: PlayerSide.Player));
        Button("Attack Opponent", () => Submit(BattleActionKind.AttackHero, selectedUnit, hero: PlayerSide.Opponent));
        GUILayout.EndHorizontal();
        if (selectedUnit != null && selectedUnit.Zone == CardZone.Field)
        {
            amount = Input("Damage / heal amount", amount);
            Button("Damage actor (target is damage source)", () => {
                if (selectedUnit.TakeDamage(Mathf.Max(0, Number(amount)), target)) game.EffectManager.ResolveOnDamageTaken(selectedUnit);
                RefreshUnit();
            });
            Button("Heal actor", () => { selectedUnit.Heal(Mathf.Max(0, Number(amount))); RefreshUnit(); });
            attack = Input("Attack / buff Attack", attack); health = Input("Health / buff Health", health);
            Button("Set actor stats (respects stat locks)", () => { selectedUnit.SetCurrentStats(Number(attack, 2), Number(health, 2), null); RefreshUnit(); });
            Button("Add permanent silenceable buff", () => { selectedUnit.AddModifier(new RuntimeModifier(Number(attack), Number(health), true)); RefreshUnit(); });
            keyword = GUILayout.SelectionGrid(keyword, Enum.GetNames(typeof(CardKeyword)), 3);
            Button("Grant selected keyword", () => { selectedUnit.GrantRuntimeKeyword((CardKeyword)keyword, int.MaxValue); RefreshUnit(); });
            Button("Freeze actor", () => { selectedUnit.ApplyFreeze(); RefreshUnit(); });
            Button("Ready actor (clear freeze / attack use)", () => { SetField(selectedUnit, "frozen", false); SetField(selectedUnit, "freezeReachedOwnTurn", false); selectedUnit.ResetForTurn(); selectedUnit.EnableAttack(); RefreshUnit(); });
            Button("Silence actor", () => { selectedUnit.Silence(); RefreshUnit(); });
            Button("Kill actor (normal death effects)", () => { selectedUnit.Kill(target); RefreshUnit(); });
        }
        Heading("Pending choices — works for either player");
        var pending = BattleChoiceRequests.Pending;
        if (pending != null)
        {
            GUILayout.Label(pending.owner + " must choose a card:");
            if (pending.owner == PlayerSide.Player) GUILayout.Label("Use the normal card-choice popup on the board (hide this panel if needed).");
            else foreach (var card in pending.cards.ToArray())
                Button(Label(card), () => BattleActions.Submit(pending.owner, BattleActionKind.ChooseCard, target: card, choice: pending.id));
        }
        var targeting = EffectTargetManager.Instance;
        if (targeting != null && targeting.HasPendingTarget)
        {
            GUILayout.Label(targeting.PendingOwner + " must choose a target:");
            foreach (PlayerSide seat in Enum.GetValues(typeof(PlayerSide)))
            {
                foreach (var card in game.GetBattlefield(seat).Minions.ToArray())
                    if (targeting.CanChoose(card)) Button(Label(card), () => BattleActions.Submit(targeting.PendingOwner, BattleActionKind.ChooseUnit, target: card));
                if (targeting.CanChooseHero(game.GetPlayerView(seat)))
                    Button(seat + " player", () => BattleActions.Submit(targeting.PendingOwner, BattleActionKind.ChooseHero, hero: seat));
            }
        }
        Heading("Setup cleanup (no death effects)");
        Button("Clear selected side's field", () => ClearZone(true));
        Button("Clear selected side's hand", () => ClearZone(false));
        Heading("Event / Console log");
        GUILayout.BeginHorizontal(); Button("Clear log", () => logs.Clear()); Button("Copy log", () => GUIUtility.systemCopyBuffer = string.Join("\n", logs)); GUILayout.EndHorizontal();
        logScroll = GUILayout.BeginScrollView(logScroll, GUILayout.Height(180));
        foreach (var line in logs.ToArray()) GUILayout.Label(line);
        GUILayout.EndScrollView(); GUILayout.EndScrollView(); GUILayout.EndArea();
    }
}
#endif
