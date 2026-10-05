using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class PlayerView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private PlayerSide side;
    [SerializeField] private int maxHealth = 20;
    private int currentHealth;

    public void ApplyRemoteHealth(int health) { currentHealth = Mathf.Clamp(health, 0, maxHealth); RefreshHealth(); }
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    [SerializeField] private TMP_Text healthText;

    [SerializeField] private Image playerIcon;
    public PlayerSide Side => side;

    public void RefreshPlayerIcon()
    {
        if (side != PlayerSide.Player || playerIcon == null) return;
        var catalog = Resources.Load<PlayerIconCatalog>("PlayerIconCatalog");
        Sprite selected = catalog == null ? null : catalog.SelectedSprite();
        if (selected != null) { playerIcon.sprite = selected; playerIcon.preserveAspect = true; }
    }

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
        RefreshPlayerIcon();
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
            currentHealth.ToString();
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