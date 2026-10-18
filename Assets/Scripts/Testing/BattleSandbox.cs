#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// A headless battle for automated tests: the real managers wired together with no scene,
/// prefabs or UI. Create one per scenario and Dispose it afterwards.
/// Editor-only, so none of this ships in player builds.
/// </summary>
public sealed class BattleSandbox : IDisposable
{
    private const BindingFlags AnyInstance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly List<Object> created = new List<Object>();
    private readonly GameManager previousGame;
    private int cardCounter;

    public GameManager Game { get; }
    public TurnManager Turns => Game.TurnManager;
    public CombatManager Combat => Game.CombatManager;

    public BattleSandbox()
    {
        previousGame = GameManager.Instance;

        Game = Add<GameManager>("GameManager");
        SetField(Game, "turnManager", Add<TurnManager>("TurnManager"));
        SetField(Game, "combatManager", Add<CombatManager>("CombatManager"));
        SetField(Game, "effectManager", Add<EffectManager>("EffectManager"));

        SetField(Game, "deckManager", SideComponent<DeckManager>(PlayerSide.Player, "owner"));
        SetField(Game, "opponentDeckManager", SideComponent<DeckManager>(PlayerSide.Opponent, "owner"));
        SetField(Game, "handManager", SideComponent<HandManager>(PlayerSide.Player, "owner"));
        SetField(Game, "opponentHandManager", SideComponent<HandManager>(PlayerSide.Opponent, "owner"));
        SetField(Game, "playerBattlefieldManager", SideComponent<BattlefieldManager>(PlayerSide.Player, "side"));
        SetField(Game, "opponentBattlefieldManager", SideComponent<BattlefieldManager>(PlayerSide.Opponent, "side"));

        SetStaticField(typeof(GameManager), "<Instance>k__BackingField", Game);
        BattleActions.ResetMatch("sandbox-match");
        BeginTurn(PlayerSide.Player, 10);
    }

    public void Dispose()
    {
        foreach (Object obj in created)
            if (obj != null)
                Object.DestroyImmediate(obj);
        created.Clear();

        ResetDescribedSpellState();
        BattleChoiceRequests.Clear();
        BattleActions.ResetMatch();
        SetStaticField(typeof(GameManager), "<Instance>k__BackingField", previousGame);
    }

    // =========================================
    // TURN AND MANA
    // =========================================

    /// <summary>Makes it <paramref name="side"/>'s turn with full mana, without drawing or turn-start effects.</summary>
    public void BeginTurn(PlayerSide side, int mana)
    {
        SetField(Turns, "currentSide", side);
        SetField(Turns, "turnNumber", 1);
        SetField(Turns, "timerRunning", true);
        SetMana(PlayerSide.Player, mana);
        SetMana(PlayerSide.Opponent, mana);
    }

    public void SetMana(PlayerSide side, int mana)
    {
        string prefix = side == PlayerSide.Player ? "player" : "opponent";
        SetField(Turns, prefix + "Mana", mana);
        SetField(Turns, prefix + "MaxMana", mana);
    }

    // =========================================
    // CARD DATA
    // =========================================

    public MonsterData UnitData(int attack, int health, params CardKeyword[] keywords) =>
        MinionData<MonsterData>(attack, health, keywords);

    public ApostleData ApostleData(int attack, int health, params CardKeyword[] keywords) =>
        MinionData<ApostleData>(attack, health, keywords);

    public ArtifactData ArtifactData(int attackBonus = 0, int healthBonus = 0, CardEffect deathrattle = null)
    {
        var data = CardData<ArtifactData>("Test Artifact");
        SetField(data, "attackBonus", attackBonus);
        SetField(data, "healthBonus", healthBonus);
        SetField(data, "deathrattle", deathrattle);
        return data;
    }

    public T CardData<T>(string name, int manaCost = 0) where T : CardData
    {
        var data = Create<T>();
        data.name = name;
        data.cardName = name;
        data.manaCost = manaCost;
        SetField(data, "cardID", "TEST_" + (++cardCounter));
        return data;
    }

    public T Effect<T>(EffectTargetType targetType = EffectTargetType.None) where T : CardEffect
    {
        var effect = Create<T>();
        SetField(effect, "targetType", targetType);
        return effect;
    }

    public ScenarioProbeEffect Probe() => Effect<ScenarioProbeEffect>();

    private T MinionData<T>(int attack, int health, CardKeyword[] keywords) where T : MinionData
    {
        var data = CardData<T>($"Test {attack}/{health}");
        data.attack = attack;
        data.health = health;
        SetField(data, "keywords", new List<CardKeyword>(keywords));
        return data;
    }

    // =========================================
    // CARDS AND ZONES
    // =========================================

    public RuntimeCard Card(CardData data, PlayerSide owner)
    {
        var card = new RuntimeCard(data);
        card.SetOwner(owner);
        return card;
    }

    /// <summary>Puts a unit straight onto the field (no Battlecry). Ready units can attack this turn.</summary>
    public RuntimeCard Summon(CardData data, PlayerSide owner, bool ready = true)
    {
        RuntimeCard card = Card(data, owner);
        var minions = (List<RuntimeCard>)GetField(Game.GetBattlefield(owner), "minions");
        minions.Add(card);
        card.ChangeZone(CardZone.Field);
        card.InitializeCombatStats();

        if (ready)
            card.ResetForTurn();

        return card;
    }

    public RuntimeCard Summon(int attack, int health, PlayerSide owner, params CardKeyword[] keywords) =>
        Summon(UnitData(attack, health, keywords), owner);

    public RuntimeCard GiveToHand(CardData data, PlayerSide owner)
    {
        RuntimeCard card = Card(data, owner);
        card.ChangeZone(CardZone.Hand);
        Expect(Game.GetHandManager(owner).AddCard(card), $"{data.cardName} could not be added to {owner}'s hand.");
        return card;
    }

    public void FillHand(PlayerSide owner)
    {
        HandManager hand = Game.GetHandManager(owner);
        int maxHandSize = (int)GetField(hand, "maxHandSize");

        while (hand.HandSize < maxHandSize)
            GiveToHand(CardData<SpellData>("Filler"), owner);
    }

    public bool IsOnField(RuntimeCard card) =>
        card.Zone == CardZone.Field && Game.GetBattlefield(card.Owner).Minions.ContainsCard(card);

    public bool IsInHand(RuntimeCard card) => Game.GetHandManager(card.Owner).Hand.ContainsCard(card);

    // =========================================
    // ACTIONS (through the same entry point as player input)
    // =========================================

    public BattleActionResult Act(PlayerSide seat, BattleActionKind kind, RuntimeCard card = null,
        RuntimeCard target = null, PlayerSide hero = PlayerSide.Opponent)
    {
        var request = new BattleActionRequest
        {
            requestID = Guid.NewGuid().ToString("N"),
            matchID = BattleActions.MatchID,
            turn = Turns.TurnNumber,
            kind = kind,
            cardID = card?.InstanceID,
            targetID = target?.InstanceID,
            hero = hero,
        };
        return BattleActions.Execute(seat, request);
    }

    public bool Play(RuntimeCard card) => Act(card.Owner, BattleActionKind.PlayCard, card).accepted;

    public bool Attack(RuntimeCard attacker, RuntimeCard defender) =>
        Act(attacker.Owner, BattleActionKind.AttackUnit, attacker, defender).accepted;

    public bool EndTurn(PlayerSide seat) => Act(seat, BattleActionKind.EndTurn).accepted;

    // =========================================
    // CHECKS
    // =========================================

    public static void Expect(bool condition, string message)
    {
        if (!condition)
            throw new BattleScenarioFailure(message);
    }

    public static void ExpectEqual<T>(T expected, T actual, string what)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new BattleScenarioFailure($"{what}: expected {expected}, got {actual}.");
    }

    // =========================================
    // REFLECTION HELPERS
    // =========================================

    /// <summary>Calls a private method; useful for testing setup steps that have no public entry point.</summary>
    public static object Invoke(object target, string method, params object[] args)
    {
        MethodInfo info = target.GetType().GetMethod(method, AnyInstance);
        if (info == null)
            throw new MissingMethodException(target.GetType().Name, method);
        return info.Invoke(target, args);
    }

    public static object GetField(object target, string field) => FindField(target.GetType(), field).GetValue(target);

    public static void SetField(object target, string field, object value) =>
        FindField(target.GetType(), field).SetValue(target, value);

    // Private fields are declared per class, so walk up the hierarchy (MonsterData -> MinionData -> CardData).
    private static FieldInfo FindField(Type type, string field)
    {
        for (Type current = type; current != null; current = current.BaseType)
        {
            FieldInfo info = current.GetField(field, AnyInstance | BindingFlags.DeclaredOnly);
            if (info != null)
                return info;
        }
        throw new MissingFieldException(type.Name, field);
    }

    private static void SetStaticField(Type type, string field, object value) =>
        type.GetField(field, BindingFlags.Static | BindingFlags.NonPublic).SetValue(null, value);

    private T Add<T>(string name) where T : Component
    {
        var go = new GameObject("Sandbox " + name);
        created.Add(go);
        return go.AddComponent<T>();
    }

    private T SideComponent<T>(PlayerSide side, string sideField) where T : Component
    {
        T component = Add<T>($"{typeof(T).Name} ({side})");
        SetField(component, sideField, side);
        FieldInfo gameField = typeof(T).GetField("gameManager", AnyInstance);
        gameField?.SetValue(component, Game);
        return component;
    }

    private T Create<T>() where T : ScriptableObject
    {
        var obj = ScriptableObject.CreateInstance<T>();
        created.Add(obj);
        return obj;
    }

    // DescribedSpellState keeps a static instance between battles.
    private static void ResetDescribedSpellState()
    {
        var instance = (DescribedSpellState)typeof(DescribedSpellState)
            .GetField("instance", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        if (instance != null)
            Object.DestroyImmediate(instance.gameObject);
        SetStaticField(typeof(DescribedSpellState), "instance", null);
    }
}

public sealed class BattleScenarioFailure : Exception
{
    public BattleScenarioFailure(string message)
        : base(message) { }
}
#endif
