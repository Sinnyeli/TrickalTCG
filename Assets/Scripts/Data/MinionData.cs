using System.Collections.Generic;
using UnityEngine;

public abstract class MinionData : CardData
{
    [Header("Stats")]
    public int attack;
    public int health;

    [Header("Race")]
    public CardRace cardRace;
    public List<CardRace> additionalRaces = new List<CardRace>();
    public bool HasRace(CardRace race) => cardRace == race || (additionalRaces != null && additionalRaces.Contains(race));

    [Header("Keywords")]
    [SerializeField]
    private List<CardKeyword> keywords =
        new List<CardKeyword>();

    public IReadOnlyList<CardKeyword> Keywords =>
        keywords;

    public bool HasKeyword(
        CardKeyword keyword)
    {
        return keywords != null &&
               keywords.Contains(keyword);
    }

    // =========================================
    // MINION TRIGGERS
    // =========================================

    [Header("Minion Effects")]
    [SerializeField]
    private CardEffect battlecry;

    [SerializeField]
    private CardEffect deathrattle;

    [SerializeField]
    private CardEffect passive;

    // Additional passives, including event-driven TriggeredEffect assets.
    // The original passive field remains serialized for existing cards.
    [SerializeField]
    private List<CardEffect> additionalPassives = new List<CardEffect>();

    [SerializeField]
    private CardEffect resonance;

    [SerializeField]
    private CardEffect turnStart;

    [SerializeField]
    private CardEffect turnEnd;

    [SerializeField]
    private CardEffect onDamageTaken;

    [SerializeField]
    private CardEffect onAttack;

    public CardEffect Battlecry => battlecry;
    public CardEffect Deathrattle => deathrattle;
    public CardEffect Passive => passive;
    public IEnumerable<CardEffect> Passives
    {
        get
        {
            if (passive != null)
                yield return passive;

            if (additionalPassives == null)
                yield break;

            foreach (CardEffect effect in additionalPassives)
            {
                if (effect != null)
                    yield return effect;
            }
        }
    }
    public CardEffect Resonance => resonance;
    public CardEffect TurnStart => turnStart;
    public CardEffect TurnEnd => turnEnd;
    public CardEffect OnDamageTaken => onDamageTaken;
    public CardEffect OnAttack => onAttack;
    // A private runtime definition: never mutates the collectible/template assets.
    public MinionData CreateFusionDefinition(List<RuntimeCard> units)
    {
        var result = Instantiate(this);
        result.attack = result.health = 1;
        result.keywords = new List<CardKeyword>();
        result.passive = null;
        result.additionalPassives = new List<CardEffect>();
        var slots = new List<CardEffect>[7];
        for (int i = 0; i < slots.Length; i++) slots[i] = new List<CardEffect>();
        foreach (var unit in units)
        {
            foreach (CardKeyword keyword in System.Enum.GetValues(typeof(CardKeyword)))
                if (unit.HasKeyword(keyword) && !result.keywords.Contains(keyword)) result.keywords.Add(keyword);
            if (unit.IsSilenced || !(unit.Data is MinionData data)) continue;
            foreach (var effect in data.Passives) result.additionalPassives.Add(Instantiate(effect));
            var effects = new[] { data.Battlecry, data.Deathrattle, data.Resonance, data.TurnStart, data.TurnEnd, data.OnDamageTaken, data.OnAttack };
            for (int i = 0; i < effects.Length; i++) if (effects[i] != null) slots[i].Add(effects[i]);
        }
        result.battlecry = FusionSequenceEffect.Create(slots[0]);
        result.deathrattle = FusionSequenceEffect.Create(slots[1]);
        result.resonance = FusionSequenceEffect.Create(slots[2]);
        result.turnStart = FusionSequenceEffect.Create(slots[3]);
        result.turnEnd = FusionSequenceEffect.Create(slots[4]);
        result.onDamageTaken = FusionSequenceEffect.Create(slots[5]);
        result.onAttack = FusionSequenceEffect.Create(slots[6]);
        return result;
    }
}
