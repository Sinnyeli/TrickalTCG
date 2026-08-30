using UnityEngine;


public class RuntimeCard
{
    private int currentHealth;
    private bool canAttack;
    public int CurrentHealth => currentHealth;
    public bool CanAttack => canAttack;
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
        if (Data is MonsterData monster)
        {
            currentHealth = monster.health;
        }
        else if (Data is ApostleData apostle)
        {
            currentHealth = apostle.health;
        }

        canAttack = false;
    }
    public void EnableAttack()
    {
        canAttack = true;
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
    public void ResetForTurn()
    {
        canAttack = true;
    }
}