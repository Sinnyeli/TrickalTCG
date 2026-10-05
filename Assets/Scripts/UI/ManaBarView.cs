using UnityEngine;
using UnityEngine.UI;

public class ManaBarView : MonoBehaviour
{
    [SerializeField] private Image[] slots = new Image[10];
    [SerializeField] private Sprite availableSprite;
    [SerializeField] private Sprite unavailableSprite;
    [SerializeField] private Color lockedColor = new Color(.45f, .45f, .45f, 1f);
    private PlayerView playerView;
    private int lastMana = -1, lastMaximum = -1;
    private PlayerSide lastSide;

    private void Awake()
    {
        playerView = GetComponentInParent<PlayerView>();
        foreach (Image slot in slots) if (slot != null) slot.raycastTarget = false;
    }
    private void OnEnable() { lastMana = lastMaximum = -1; }
    private void LateUpdate()
    {
        if (playerView == null) return;
        TurnManager turns = GameManager.Instance == null ? null : GameManager.Instance.TurnManager;
        int maximum = turns == null ? 0 : Mathf.Clamp(turns.GetMaxMana(playerView.Side), 0, 10);
        int mana = turns == null ? 0 : Mathf.Clamp(turns.GetMana(playerView.Side), 0, maximum);
        if (mana == lastMana && maximum == lastMaximum && playerView.Side == lastSide) return;
        lastMana = mana; lastMaximum = maximum; lastSide = playerView.Side;
        for (int i = 0; i < slots.Length; i++)
        {
            Image slot = slots[i];
            if (slot == null) continue;
            bool available = i < mana;
            slot.sprite = available ? availableSprite : unavailableSprite;
            // Empty but unlocked mana keeps the off image; locked mana is darker.
            slot.color = i < maximum ? Color.white : lockedColor;
        }
    }
}
