using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

public class RuntimeCard
{
    private bool destroyAtTurnEnd;
    private RuntimeCard turnEndDeathSource;
    public void ScheduleDeathAtTurnEnd(RuntimeCard source)
    {
        destroyAtTurnEnd = true; turnEndDeathSource = source;
    }
    private void ClearScheduledDeath() { destroyAtTurnEnd = false; turnEndDeathSource = null; }
    private readonly Dictionary<CardEffect, int> turnAbilityCounts = new Dictionary<CardEffect, int>();
    public int AdvanceTurnAbilityCounter(CardEffect effect)
    {
        turnAbilityCounts.TryGetValue(effect, out int count); turnAbilityCounts[effect] = ++count; return count;
    }
    private readonly Dictionary<CardEffect, RuntimeCard> deathMarks = new Dictionary<CardEffect, RuntimeCard>();
    public void SetDeathMark(CardEffect effect, RuntimeCard target) => deathMarks[effect] = target;
    public RuntimeCard ConsumeDeathMark(CardEffect effect)
    {
        deathMarks.TryGetValue(effect, out var target); deathMarks.Remove(effect); return target;
    }
    // Stat exchanges use visible Attack/current Health and clear old wounds after assignment.
    public void SetCurrentStats(int attack, int health, RuntimeCard source)
    {
        if (HasActiveAbility<SylphyrFixedStatsEffect>()) return;
        AddModifier(new RuntimeModifier(attack - GetAttack(), health - GetMaxHealth(), true, source));
        damageTaken = 0; currentHealth = Mathf.Max(0, GetMaxHealth());
    }
    public bool IsFusionSummon { get; private set; }
    public bool SuppressSummonBattlecry { get; private set; }
    private bool immuneUntilTurnEnd;
    private int skippedAttackTurns;
    private bool restingThisTurn;
    private readonly HashSet<CardEffect> completedAbilities = new HashSet<CardEffect>();
    public bool TryCompleteAbility(CardEffect effect) => completedAbilities.Add(effect);
    private readonly Dictionary<CardEffect, int> abilityCounts = new Dictionary<CardEffect, int>();
    public int AdvanceAbilityCounter(CardEffect effect)
    {
        abilityCounts.TryGetValue(effect, out int value);
        abilityCounts[effect] = ++value;
        return value;
    }
    public void ProtectUntilTurnEnd() => immuneUntilTurnEnd = true;
    public void SkipNextAttackTurn() => skippedAttackTurns = 1;
    public void PrepareFusion(List<RuntimeCard> inheritedArtifacts, int attack, int health)
    {
        IsFusionSummon = SuppressSummonBattlecry = true;
        // Bypass equipment capacity and on-equip rolls; each absorbed artifact keeps its abilities.
        foreach (var original in inheritedArtifacts)
        {
            if (!(original.Data is ArtifactData data)) continue;
            var artifact = new RuntimeCard(data.CreateFusionCopy());
            artifact.SetOwner(Owner); artifact.ChangeZone(CardZone.Field);
            equippedArtifacts.Add(artifact);
        }
        modifiers.Add(new RuntimeModifier(attack - 1, health - 1, true, this));
    }
    public void CompleteFusion(int attack, int health)
    {
        // Account for inherited auras already reapplied by PlayCard, without firing stat-gain triggers.
        modifiers.Add(new RuntimeModifier(attack - GetAttack(), health - GetMaxHealth(), true, this));
        damageTaken = 0; currentHealth = GetMaxHealth();
        SuppressSummonBattlecry = false;
    }
    public static event System.Action<CardTriggerType, RuntimeCard, RuntimeCard, RuntimeModifier> OnGameplayTrigger;
    private readonly Dictionary<TriggeredEffect, int> triggerCounts = new Dictionary<TriggeredEffect, int>();
    public RuntimeCard LastDamageSource { get; private set; }
    public RuntimeModifier LethalStatSnapshot { get; private set; }
    public int CountTrigger(TriggeredEffect effect)
    {
        triggerCounts.TryGetValue(effect, out int count);
        triggerCounts[effect] = ++count;
        return count;
    }
    public void ResetTriggerCount(TriggeredEffect effect) => triggerCounts.Remove(effect);
    public static void PublishTrigger(CardTriggerType type, RuntimeCard actor, RuntimeCard subject, RuntimeModifier snapshot = null)
        => OnGameplayTrigger?.Invoke(type, actor, subject, snapshot);
    private int currentHealth;
    private bool canAttack;
    public int CurrentHealth => currentHealth;
    private int damageTaken;
    public int DamageTaken => damageTaken;
    public bool CanAttack => canAttack && !IsFrozen && !restingThisTurn;
    private bool cannotAttackHero;
    public bool CannotAttackHero => cannotAttackHero;
    private bool isSilenced;
    public bool IsSilenced => isSilenced;
    private bool isStealthed;
    public bool IsStealthed => isStealthed;

    private int manaCostModifier = 0;

    public int GetManaCost()
{
    if (Data == null)
        return 0;

    int baseCost = Data.manaCost;
    if (Zone == CardZone.Hand && Data is ApostleData apostle && apostle.PlayRule != null)
        baseCost = apostle.PlayRule.GetBaseCost(this);
    return Mathf.Max(
        0,
        baseCost + manaCostModifier -
        (Zone == CardZone.Hand && GameManager.Instance != null ? GameManager.Instance.GetNextCardDiscount(Owner) : 0) -
        (Data is SpellData && GameManager.Instance != null ? GameManager.Instance.GetPermanentSpellDiscount(Owner) + GameManager.Instance.GetWitchSpellAuraDiscount(Owner) : 0)
    );
}
    private bool resolvingPassive = false;

    public bool IsResolvingPassive =>
        resolvingPassive;

    public void SetResolvingPassive(bool value)
    {
        resolvingPassive = value;
    }

    private int? baseAttackOverride;
    private int? baseHealthOverride;

    private RuntimeCard baseStatOverrideSource;
    private bool baseStatOverrideIsPassive;

    private List<RuntimeCard> equippedArtifacts =
        new List<RuntimeCard>();

    public IReadOnlyList<RuntimeCard> EquippedArtifacts =>
        equippedArtifacts;
    public CardData Data { get; private set; }

    public bool IsCommander { get; private set; }

    public CardZone Zone { get; private set; }

    private bool frozen;
    private bool freezeReachedOwnTurn;
    public bool IsFrozen => frozen;
    private sealed class KeywordGrant
    {
        public CardKeyword Keyword;
        public int TurnEnds;
    }
    private readonly List<KeywordGrant> runtimeKeywords = new List<KeywordGrant>();
    private readonly Dictionary<RuntimeModifier, int> timedModifiers = new Dictionary<RuntimeModifier, int>();

    public void ApplyFreeze()
    {
        frozen = true;
        freezeReachedOwnTurn = false;
    }
    public void GrantRuntimeKeyword(CardKeyword keyword, int turnEnds)
    {
        runtimeKeywords.Add(new KeywordGrant { Keyword = keyword, TurnEnds = turnEnds });
        if (keyword == CardKeyword.Stealth) ActivateStealth();
        if (keyword == CardKeyword.Rush && !hasAttackedThisTurn) EnableAttack();
    }
    public void AddTimedModifier(RuntimeModifier modifier, int turnEnds)
    {
        timedModifiers[modifier] = Mathf.Max(1, turnEnds);
        AddModifier(modifier);
        RecalculateCurrentHealth();
    }
    public void FinishAbilityTurn(PlayerSide endingSide)
    {
        immuneUntilTurnEnd = false;
        if (endingSide == Owner) { restingThisTurn = false; turnAbilityCounts.Clear(); }
        foreach (var modifier in new List<RuntimeModifier>(timedModifiers.Keys))
        {
            int remaining = timedModifiers[modifier] - 1;
            if (remaining <= 0) { modifiers.Remove(modifier); timedModifiers.Remove(modifier); }
            else timedModifiers[modifier] = remaining;
        }
        for (int i = runtimeKeywords.Count - 1; i >= 0; i--)
        {
            var grant = runtimeKeywords[i];
            if (grant.TurnEnds == 0) continue;
            if (--grant.TurnEnds <= 0) runtimeKeywords.RemoveAt(i);
        }
        if (!HasKeyword(CardKeyword.Stealth)) RemoveStealth();
        if (endingSide == Owner && freezeReachedOwnTurn)
        {
            frozen = false;
            freezeReachedOwnTurn = false;
        }
        RecalculateCurrentHealth();
        if (destroyAtTurnEnd && Zone == CardZone.Field)
        {
            var deathSource = turnEndDeathSource; ClearScheduledDeath();
            Kill(deathSource); // TurnManager checks death immediately after finishing this unit's turn effects.
        }
    }

    public PlayerSide Owner { get; private set; }
    private List<RuntimeModifier> modifiers = new List<RuntimeModifier>();
    public static event System.Action<RuntimeCard, RuntimeModifier> OnStatsGained;
    private bool hasAttackedThisTurn;

    public bool HasAttackedThisTurn =>
        hasAttackedThisTurn;
    
    public void MarkAttacked()
    {
        hasAttackedThisTurn = true;
    }
    public int GetMaxEquipment()
    {
        if (Data is ApostleData apostleData)
        {
            return apostleData.MaxEquipment;
        }

        return 0;
    }

    public RuntimeCard(CardData data, bool isCommander = false)
    {
        Data = data;
        IsCommander = isCommander;
        Zone = CardZone.Deck;
    }

    public void ChangeZone(CardZone newZone)
    {
        if (Zone == CardZone.Field && newZone != CardZone.Field)
        {
            ClearScheduledDeath();
            frozen = false; freezeReachedOwnTurn = false;
            runtimeKeywords.Clear();
            foreach (var modifier in timedModifiers.Keys) modifiers.Remove(modifier);
            timedModifiers.Clear();
        }
        Zone = newZone;
    }


    public void SetOwner(PlayerSide owner)
    {
        Owner = owner;
    }


    public void InitializeCombatStats()
    {
        currentHealth = GetMaxHealth();
        damageTaken = 0;

     if (HasKeyword(CardKeyword.Rush))
    {
        canAttack = HasKeyword(CardKeyword.Rush); // if true it can attack
        cannotAttackHero = true;
    }
    if (HasKeyword(CardKeyword.Stealth))
        {
            ActivateStealth();
        }
    else
    {
        canAttack = HasKeyword(CardKeyword.Rush); // if false, it has no rush and can't attack anyways. Rather have additional check.
        cannotAttackHero = true;
    }
        
    }

    public bool HasKeyword(
        CardKeyword keyword)
    {
        if (runtimeKeywords.Exists(grant => grant.Keyword == keyword)) return true;
        // Intrinsic minion keyword
        if (!isSilenced && Data is MinionData minionData &&
            minionData.HasKeyword(keyword))
        {
            return true;
        }

        // Artifact-granted keyword
        foreach (RuntimeCard artifact in equippedArtifacts)
        {
            if (artifact == null)
                continue;

            if (artifact.Data is ArtifactData artifactData &&
                artifactData.GrantsKeyword(keyword))
            {
                return true;
            }
        }

        return false;
    }
    public void EnableAttack()
    {
        canAttack = true;
    }
     public void DisableAttack()
    {
        canAttack = false;
    }

    public bool TakeDamage(int amount, RuntimeCard damageSource = null)
        => TakeDamageInternal(amount, damageSource, true);

    private bool TakeDamageInternal(int amount, RuntimeCard damageSource, bool allowRedirect)
    {
        if (immuneUntilTurnEnd && amount > 0) return false;
        if (amount <= 0)
            return false;

        var game = GameManager.Instance;
        if (allowRedirect && Zone == CardZone.Field && game != null)
        {
            var field = game.GetBattlefield(Owner);
            if (field != null)
            {
                // Snapshot: damage events can change the battlefield during resolution.
                foreach (var protector in new List<RuntimeCard>(field.Minions))
                {
                    if (protector == null || !(protector.Data is MinionData data)) continue;
                    foreach (var passive in data.Passives)
                    {
                        if (!(passive is DamageRedirectEffect redirect) ||
                            !redirect.Protects(protector, this)) continue;
                        // No redirection chains: the receiving unit applies its own mitigation.
                        bool received = protector.TakeDamageInternal(amount, damageSource, false);
                        if (received && protector.CurrentHealth > 0)
                            game.EffectManager.ResolveOnDamageTaken(protector);
                        field.RefreshMinionView(protector);
                        game.CombatManager.CheckDeath(protector);
                        // The original target took no damage: do not fire its damage reactions.
                        return false;
                    }
                }
            }
        }

        int actualDamage = amount;
        if (!IsSilenced && Data is MinionData penaltyData)
            foreach (var passive in penaltyData.Passives)
                if (passive is OpalDamagePenaltyEffect penalty) actualDamage += penalty.ExtraDamage;

        if (HasKeyword(CardKeyword.Endure) &&
            (damageSource == null || !damageSource.HasMayoEquipment(MayoEquipmentEffect.Ability.IgnoreEndure)))
        {
            actualDamage -= 1;

            if (actualDamage < 0)
            {
                actualDamage = 0;
            }
        }

        if (actualDamage <= 0) return false;
        LethalStatSnapshot = null;
        if (actualDamage >= currentHealth)
            LethalStatSnapshot = new RuntimeModifier(GetAttack(), GetMaxHealth(), false);
        LastDamageSource = damageSource;
        damageTaken += actualDamage;

        currentHealth -= actualDamage;

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }
        PublishTrigger(CardTriggerType.DamageTaken, damageSource, this);
        if (damageSource != null) PublishTrigger(CardTriggerType.DamageDealt, damageSource, this);
        return true;
    }
    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        int before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, GetMaxHealth());
        damageTaken = Mathf.Max(0, damageTaken - Mathf.Max(0, currentHealth - before));
    }

   private int GetBaseAttack()
{
    if (baseAttackOverride.HasValue)
        return baseAttackOverride.Value;

    if (Data is MinionData minion)
        return minion.attack;

    return 0;
}

private int GetBaseHealth()
{
    if (baseHealthOverride.HasValue)
        return baseHealthOverride.Value;

    if (Data is MinionData minion)
        return minion.health;

    return 0;
}

        public void SetBaseStatOverride(
            int attack,
            int health,
            RuntimeCard source,
            bool isPassive)
        {
            int oldMaxHealth = GetMaxHealth();

            baseAttackOverride = attack;
            baseHealthOverride = health;

            baseStatOverrideSource = source;
            baseStatOverrideIsPassive = isPassive;

            int newMaxHealth = GetMaxHealth();

            // Increase current health by the amount max health increased.
            if (newMaxHealth > oldMaxHealth)
            {
                currentHealth += newMaxHealth - oldMaxHealth;
            }
            else if (currentHealth > newMaxHealth)
            {
                currentHealth = newMaxHealth;
            }
        }
    public void ClearPassiveBaseStatOverride()
    {
        
        if (!baseStatOverrideIsPassive)
            return;

        baseAttackOverride = null;
        baseHealthOverride = null;
        baseStatOverrideSource = null;
        baseStatOverrideIsPassive = false;

        int maxHealth = GetMaxHealth();

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

            Debug.Log(
                $"{Data.cardName}: After clearing passive = " +
                $"{GetAttack()}/{CurrentHealth} " +
                $"(Max HP: {GetMaxHealth()})"
);
    }

    public bool HasActiveAbility<T>() where T : CardEffect
    {
        if (IsSilenced || !(Data is MinionData minion)) return false;
        foreach (var effect in minion.Passives) if (effect is T) return true;
        return false;
    }
    private bool applyingRuddBonus;
    public bool HasMayoEquipment(MayoEquipmentEffect.Ability kind)
    {
        if (IsSilenced) return false;
        foreach (var artifact in equippedArtifacts)
            if (artifact.Data is ArtifactData data && data.ArtifactEffect is MayoEquipmentEffect effect && effect.Kind == kind) return true;
        return false;
    }
    public void ResolveMayoAttack(RuntimeCard defender)
    {
        if (defender != null && defender.Zone == CardZone.Field && defender.CurrentHealth > 0 &&
            HasMayoEquipment(MayoEquipmentEffect.Ability.FreezeOnAttack)) defender.ApplyFreeze();
    }
    public void CopyEquipmentStats(int attack, int health, RuntimeCard artifact)
    {
        if (HasActiveAbility<SylphyrFixedStatsEffect>()) return;
        AddModifier(new RuntimeModifier(attack - GetAttack(), health - GetMaxHealth(), true, artifact));
        damageTaken = 0;
        currentHealth = Mathf.Max(0, GetMaxHealth());
    }
    public int GetCombatDamage()
    {
        var field = GameManager.Instance?.GetBattlefield(Owner);
        if (field != null)
            foreach (var ally in field.Minions)
                if (ally != null && ally.CurrentHealth > 0 && ally.HasActiveAbility<ViviHealthCombatEffect>())
                    return Mathf.Max(0, CurrentHealth);
        return GetAttack();
    }
    public void ResolveBeforeDefending()
    {
        if (IsSilenced || !(Data is MinionData minion)) return;
        foreach (var effect in minion.Passives)
            if (effect is RitzDefendSwapEffect swap) { swap.SwapBeforeDefense(this); break; }
    }

        public int GetAttack()
        {
            if (HasActiveAbility<SylphyrFixedStatsEffect>()) return 3;
            int attack = GetBaseAttack();

            foreach (RuntimeModifier modifier in modifiers)
                attack += modifier.AttackBonus;

            foreach (RuntimeCard artifact in equippedArtifacts)
            {
                if (artifact == null)
                    continue;

                if (artifact.Data is ArtifactData artifactData)
                    attack += artifactData.AttackBonus;
            }

            if (!IsSilenced && Data is MinionData minion)
                foreach (var passive in minion.Passives)
                    if (passive is ConditionalStatEffect conditional)
                        attack += conditional.GetAttackContribution(this);
            return Mathf.Max(0, attack);
        }

       public int GetMaxHealth()
        {
            if (HasActiveAbility<SylphyrFixedStatsEffect>()) return 3;
            int health = GetBaseHealth();

            foreach (RuntimeModifier modifier in modifiers)
                health += modifier.HealthBonus;

            foreach (RuntimeCard artifact in equippedArtifacts)
            {
                if (artifact == null)
                    continue;

                if (artifact.Data is ArtifactData artifactData)
                    health += artifactData.HealthBonus;
            }

            return health;
        }


public void AddModifier(
    RuntimeModifier modifier)
{
    if (modifier == null)
        return;

    if (HasActiveAbility<SylphyrFixedStatsEffect>()) return;
    modifiers.Add(
        modifier
    );
    // =========================================
    // HEALTH
    // =========================================

    // Health buffs also increase current health.
    if (modifier.HealthBonus > 0)
    {
        currentHealth +=
            modifier.HealthBonus;
    }


    // =========================================
    // STAT GAIN EVENT
    // =========================================

   if ((modifier.AttackBonus > 0 ||
     modifier.HealthBonus > 0) &&
    !modifier.IsPassive)
    {
        OnStatsGained?.Invoke(
            this,
            modifier
        );
        if (!applyingRuddBonus && HasActiveAbility<RuddStatGainEffect>())
        {
            applyingRuddBonus = true;
            try { AddModifier(new RuntimeModifier(1, 1, true, this)); }
            finally { applyingRuddBonus = false; }
        }

    }
}


        public void ModifyManaCost(int amount)
        {
            manaCostModifier += amount;
        }

        public void SetManaCostModifier(int amount)
        {
            manaCostModifier = amount;
        }

        public void ResetManaCostModifier()
        {
            manaCostModifier = 0;
        }
    public void RemoveSilenceableModifiers()
    {
        modifiers.RemoveAll(
            modifier => modifier.CanBeSilenced
        );

        int maxHealth = GetMaxHealth();

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }

    public void ResetForTurn()
    {
        if (frozen) freezeReachedOwnTurn = true;
        restingThisTurn = skippedAttackTurns > 0;
        canAttack = !restingThisTurn;
        if (skippedAttackTurns > 0) skippedAttackTurns--;
        cannotAttackHero = false;
         hasAttackedThisTurn = false;

    }

    public void RemoveModifiersFromSource(RuntimeCard source)
    {
        if (source == null)
            return;

        modifiers.RemoveAll(
            modifier => modifier.Source == source
        );

        int maxHealth = GetMaxHealth();

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }

public void RemovePassiveModifiers()
{
    modifiers.RemoveAll(
        modifier => modifier.IsPassive
    );
}
public void RecalculateCurrentHealth()
{
    currentHealth =
        Mathf.Max(
            0,
            GetMaxHealth() - damageTaken
        );
}
    public void Silence()
    {
        isSilenced = true;
        deathMarks.Clear(); turnAbilityCounts.Clear(); ClearScheduledDeath();
        frozen = false; freezeReachedOwnTurn = false;
        runtimeKeywords.Clear();
        RemoveStealth();
        RemoveSilenceableModifiers();
        timedModifiers.Clear();
        immuneUntilTurnEnd = false; skippedAttackTurns = 0; restingThisTurn = false;
        if (IsFusionSummon)
        {
            equippedArtifacts.Clear();
            modifiers.Clear(); ClearPassiveBaseStatOverride();
            damageTaken = 0; currentHealth = 1;
        }
    }
    public void ActivateStealth()
        {
            isStealthed = true;
        }

    public void RemoveStealth()
        {
            isStealthed = false;
        }




  public bool EquipArtifact(RuntimeCard artifact)
    {
        if (artifact == null)
            return false;

        if (!(Data is ApostleData))
        {
            Debug.Log(
                $"{Data.cardName} cannot equip artifacts."
            );
            return false;
        }

        if (!(artifact.Data is ArtifactData))
        {
            Debug.Log(
                $"{artifact.Data.cardName} is not an artifact."
            );
            return false;
        }

        int maxEquipment = GetMaxEquipment();

        if (equippedArtifacts.Count >= maxEquipment)
        {
            Debug.Log(
                $"{Data.cardName} already has the maximum " +
                $"number of artifacts."
            );

            return false;
        }

        equippedArtifacts.Add(artifact);

        artifact.SetOwner(Owner);
        artifact.ChangeZone(CardZone.Field);

        if (artifact.Data is ArtifactData artifactData)
        {
            if (!HasActiveAbility<SylphyrFixedStatsEffect>()) currentHealth += artifactData.HealthBonus;

            if (artifactData.ArtifactEffect != null)
            {
                GameManager.Instance
                    .EffectManager
                    .ResolveArtifactEffect(
                        artifact,
                        artifactData.ArtifactEffect
                    );
            }
        }

        GameManager.Instance
            .GetBattlefield(Owner)
            .RefreshArtifactEffects();

        Debug.Log(
            $"{Data.cardName} equipped " +
            $"{artifact.Data.cardName}. " +
            $"Artifacts: {equippedArtifacts.Count}/{maxEquipment}"
        );

        return true;
    }    
public bool UnequipArtifact(RuntimeCard artifact)
    {
        if (artifact == null)
            return false;

        if (!equippedArtifacts.Contains(artifact))
            return false;

        equippedArtifacts.Remove(artifact);

        artifact.ChangeZone(CardZone.Graveyard);

        int maxHealth = GetMaxHealth();

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        Debug.Log(
            $"{Data.cardName} unequipped " +
            $"{artifact.Data.cardName}. " +
            $"Artifacts: {equippedArtifacts.Count}/{GetMaxEquipment()}"
        );

        return true;
    }

    public void RemoveAllArtifacts()
{
    List<RuntimeCard> artifacts =
        new List<RuntimeCard>(equippedArtifacts);

    foreach (RuntimeCard artifact in artifacts)
    {
        UnequipArtifact(artifact);
    }
}

public void Kill(RuntimeCard source = null)
{
    LethalStatSnapshot = new RuntimeModifier(GetAttack(), GetMaxHealth(), false);
    LastDamageSource = source;
    currentHealth = 0;
}
public void ResetAfterBounce()
{
    triggerCounts.Clear();
    deathMarks.Clear(); turnAbilityCounts.Clear(); ClearScheduledDeath();
    abilityCounts.Clear(); immuneUntilTurnEnd = false; skippedAttackTurns = 0; restingThisTurn = false;
    LastDamageSource = null;
    LethalStatSnapshot = null;
    modifiers.Clear();
    timedModifiers.Clear(); runtimeKeywords.Clear();
    frozen = false; freezeReachedOwnTurn = false;

    ClearPassiveBaseStatOverride();

    isSilenced = false;
    isStealthed = false;

    canAttack = false;
    cannotAttackHero = false;

    damageTaken = 0;

    currentHealth =
        GetMaxHealth();
}

public bool TransformInto(CardData newData)
{
    if (newData == null)
        return false;

    if (!(newData is MinionData))
    {
        Debug.LogWarning(
            "RuntimeCard can only transform into MinionData."
        );

        return false;
    }

    bool previousCanAttack = canAttack;
    bool previousHeroRestriction = cannotAttackHero;
    Data = newData;
    IsFusionSummon = SuppressSummonBattlecry = false;

    // Clear temporary battlefield modifications.
    triggerCounts.Clear();
    deathMarks.Clear(); turnAbilityCounts.Clear(); ClearScheduledDeath();
    abilityCounts.Clear(); immuneUntilTurnEnd = false; skippedAttackTurns = 0; restingThisTurn = false;
    LastDamageSource = null;
    LethalStatSnapshot = null;
    modifiers.Clear();
    timedModifiers.Clear(); runtimeKeywords.Clear();
    frozen = false; freezeReachedOwnTurn = false;

    ClearPassiveBaseStatOverride();

    isSilenced = false;
    isStealthed = false;

    cannotAttackHero = false;

    // Initialize using the NEW card's stats.
    InitializeCombatStats();
    canAttack = previousCanAttack;
    cannotAttackHero = previousHeroRestriction;

    return true;
}


}