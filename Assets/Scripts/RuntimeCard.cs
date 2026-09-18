using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

public class RuntimeCard
{
    private int currentHealth;
    private bool canAttack;
    public int CurrentHealth => currentHealth;
    private int damageTaken;
    public int DamageTaken => damageTaken;
    public bool CanAttack => canAttack;
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

    return Mathf.Max(
        0,
        Data.manaCost + manaCostModifier
    );
}

    private const int MaxArtifacts = 3;
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

    public PlayerSide Owner { get; private set; }
    private List<RuntimeModifier> modifiers = new List<RuntimeModifier>();
    

    public RuntimeCard(CardData data, bool isCommander = false)
    {
        Data = data;
        IsCommander = isCommander;
        Zone = CardZone.Deck;
    }

    public void ChangeZone(CardZone newZone)
    {
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

    public bool HasKeyword(CardKeyword keyword)
    {
        if (Data.HasKeyword(keyword))
            return true;

        foreach (RuntimeCard artifact in equippedArtifacts)
        {
            if (artifact == null)
                continue;

            if (artifact.Data is ArtifactData artifactData &&
                artifactData.HasKeyword(keyword))
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

    public bool TakeDamage(int amount)
    {
        if (amount <= 0)
            return false;

        int actualDamage = amount;

        if (HasKeyword(CardKeyword.Endure))
        {
            actualDamage -= 1;

            if (actualDamage < 0)
            {
                actualDamage = 0;
            }
        }

        damageTaken += amount;

        currentHealth -= actualDamage;

        if (currentHealth < 0)
        {
            currentHealth = 0;
        }
        return true;
    }
    public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        currentHealth += amount;

        int maxHealth = GetMaxHealth();

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;
    }

    private int GetBaseAttack()
    {
        if (baseAttackOverride.HasValue)
            return baseAttackOverride.Value;

        if (Data is MonsterData monster)
            return monster.attack;

        if (Data is ApostleData apostle)
            return apostle.attack;

        return 0;
    }

    private int GetBaseHealth()
    {
        if (baseHealthOverride.HasValue)
            return baseHealthOverride.Value;

        if (Data is MonsterData monster)
            return monster.health;

        if (Data is ApostleData apostle)
            return apostle.health;

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

        public int GetAttack()
        {
            int attack = GetBaseAttack();

            foreach (RuntimeModifier modifier in modifiers)
                attack += modifier.AttackBonus;

            foreach (RuntimeCard artifact in equippedArtifacts)
            {
                if (artifact == null)
                    continue;

                if (artifact.Data is ArtifactData artifactData)
                    attack += artifactData.attackBonus;
            }

            return attack;
        }

       public int GetMaxHealth()
        {
            int health = GetBaseHealth();

            foreach (RuntimeModifier modifier in modifiers)
                health += modifier.HealthBonus;

            foreach (RuntimeCard artifact in equippedArtifacts)
            {
                if (artifact == null)
                    continue;

                if (artifact.Data is ArtifactData artifactData)
                    health += artifactData.healthBonus;
            }

            return health;
        }


     public void AddModifier(RuntimeModifier modifier)
        {
            if (modifier == null)
                return;

            modifiers.Add(modifier);

            // Health buffs also increase current health.
            if (modifier.HealthBonus > 0)
            {
                currentHealth += modifier.HealthBonus;
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
        canAttack = true;
        cannotAttackHero = false;
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

        int maxHealth = GetMaxHealth();

        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }
    }
    public void Silence()
    {
        isSilenced = true;
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

        if (equippedArtifacts.Count >= MaxArtifacts)
        {
            Debug.Log(
                $"{Data.cardName} already has the maximum " +
                $"number of artifacts."
            );
            return false;
        }

        equippedArtifacts.Add(artifact);

        artifact.ChangeZone(CardZone.Field);

        if (artifact.Data is ArtifactData artifactData)
        {
            currentHealth += artifactData.healthBonus;

            if (artifactData.ArtifactEffect != null)
            {
                GameManager.Instance.EffectManager.ResolveArtifactEffect(
                    artifact,
                    artifactData.ArtifactEffect
                );
            }
        }
        GameManager.Instance.GetBattlefield(Owner).RefreshArtifactEffects();
        Debug.Log(
            $"{Data.cardName} equipped " +
            $"{artifact.Data.cardName}. " +
            $"Artifacts: {equippedArtifacts.Count}/{MaxArtifacts}"
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

    Debug.Log(
        $"{Data.cardName} unequipped " +
        $"{artifact.Data.cardName}. " +
        $"Artifacts: {equippedArtifacts.Count}/{MaxArtifacts}"
    );

    return true;
}

public void Kill()
{
    currentHealth = 0;
}


}