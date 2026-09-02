using UnityEngine;


public class RuntimeCard
{
    private int currentHealth;
    private bool canAttack;
    public int CurrentHealth => currentHealth;
    public bool CanAttack => canAttack;
    private bool cannotAttackHero;
    public bool CannotAttackHero => cannotAttackHero;
    public CardData Data { get; private set; }

    public bool IsCommander { get; private set; }

    public CardZone Zone { get; private set; }

    public PlayerSide Owner { get; private set; }
    

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
     if (HasKeyword(CardKeyword.Rush))
    {
        canAttack = HasKeyword(CardKeyword.Rush); // if true it can attack
        cannotAttackHero = true;
    }
    else
    {
        canAttack = HasKeyword(CardKeyword.Rush); // if false, it has no rush and can't attack anyways. Rather have additional check.
        cannotAttackHero = true;
    }
        
    }

    public bool HasKeyword(CardKeyword keyword)
    {
        return Data.HasKeyword(keyword); // Check keyword
    }
    public void EnableAttack()
    {
        canAttack = true;
    }
     public void DisableAttack()
    {
        canAttack = false;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        if (currentHealth < 0)
            currentHealth = 0;
    }


    public int GetAttack()
    {
        if (Data is MonsterData monster)
            return monster.attack;

        if (Data is ApostleData apostle)
            return apostle.attack;

        return 0;
    }
    public int GetMaxHealth()
    {
        if (Data is MonsterData monster)
        {
            return monster.health;
        }
        else if (Data is ApostleData apostle)
        {
            return apostle.health;
        }
    return 0;
    }

    public void ResetForTurn()
    {
        canAttack = true;
        cannotAttackHero = false;
    }
}