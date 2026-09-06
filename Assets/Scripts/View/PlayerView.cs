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
        RefreshHealth();    
    }
     
  
    public void SetSide(PlayerSide newSide)
        {
            side = newSide;
            RefreshView();
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
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        RefreshHealth();

        Debug.Log(
            $"{side} Player HP: {currentHealth}/{maxHealth}"
        );

        if (currentHealth <= 0)
        {
            GameManager.Instance.PlayerDefeated(side);
        }
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