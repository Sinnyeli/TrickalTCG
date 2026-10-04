using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class PlayerView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private PlayerSide side;
    [SerializeField] private int maxHealth = 20;
    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    [SerializeField] private TMP_Text healthText;

    public PlayerSide Side => side;

private void Awake()
{
    currentHealth = maxHealth;

    Debug.Log(
        $"PLAYER VIEW AWAKE | " +
        $"Object={gameObject.name} | " +
        $"InstanceID={GetInstanceID()} | " +
        $"Side={side} | " +
        $"HP={currentHealth}/{maxHealth}"
    );

    RefreshHealth();
}
     
  public void SetSide(PlayerSide newSide)
    {
        side = newSide;
        currentHealth = maxHealth;

        RefreshView();
        RefreshHealth();

        Debug.Log(
            $"PLAYER VIEW SET SIDE | " +
            $"Object={gameObject.name} | " +
            $"InstanceID={GetInstanceID()} | " +
            $"Side={side} | " +
            $"HP={currentHealth}/{maxHealth}"
        );
    }

    private void RefreshView()
    {
        if (side == PlayerSide.Player)
        {
            // Player-specific visual setup
//            Debug.Log("PlayerView set to PLAYER");
        }
        else if (side == PlayerSide.Opponent)
        {
            // Opponent-specific visual setup
//            Debug.Log("PlayerView set to OPPONENT");
        }
}
    private void RefreshHealth()
    {
        healthText.text =
            $"{currentHealth} / {maxHealth}";
    }
 public void TakeDamage(int damage)
{
    if (damage <= 0)
        return;

    Debug.Log(
        $"BEFORE DAMAGE: " +
        $"Side={side}, " +
        $"HP={currentHealth}, " +
        $"Damage={damage}"
    );


    currentHealth -= damage;

    currentHealth =
        Mathf.Max(
            currentHealth,
            0
        );


    Debug.Log(
        $"AFTER DAMAGE: " +
        $"Side={side}, " +
        $"HP={currentHealth}"
    );


    RefreshHealth();


    if (currentHealth <= 0)
    {
        GameManager.Instance.PlayerDefeated(
            side
        );
    }
}
        public void Heal(int amount)
    {
        if (amount <= 0)
            return;

        currentHealth += amount;

        if (currentHealth > maxHealth)
            currentHealth = maxHealth;

        RefreshHealth();
    }

    public void IncreaseMaximumHealth(int amount)
    {
        if (amount <= 0) return;
        maxHealth = (int)System.Math.Min(int.MaxValue, (long)maxHealth + amount);
        RefreshHealth();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log(
            $"PlayerView clicked: {side}"
        );
        if (EffectTargetManager.Instance != null &&
            EffectTargetManager.Instance.IsSelectingTarget)
        {
            EffectTargetManager.Instance.SelectHeroTarget(this);
            return;
        }
        CombatManager combat =
            GameManager.Instance.CombatManager;

        if (combat == null)
            return;

        combat.Attack(this);
    }
}