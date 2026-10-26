using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class MinionViewBase : MonoBehaviour
{
    [Header("UI")]
    public Image artworkImage;
    public TMP_Text nameText;
    public TMP_Text attackText;
    public TMP_Text healthText;
    public Image raceImage;
    public Image artifactOne;
    public Image artifactTwo;
    public Image artifactThree;

    [Header("Frame")]
    // Scalloped band around the portrait, tinted by side to match the board.
    public Image frameBand;
    public Sprite frameGlow;
    public GameObject raceBadge;
    public Color playerFrameColor = new Color(.55f, .75f, 1f);
    public Color enemyFrameColor = new Color(1f, .62f, .7f);

    protected RuntimeCard runtimeCard;
    public RuntimeCard RuntimeCard => runtimeCard;

    // Last values drawn, so a change can be announced (null until the first draw of a card).
    private int? shownHealth;
    private int? shownAttack;
    public int ShownHealth => shownHealth ?? 0;

    private bool isSelected;
    public bool IsSelected => isSelected;

    public void SetSelected(bool selected)
    {
        isSelected = selected;
    }

    public void SetMinion(RuntimeCard card)
    {
        if (card == null)
        {
            Debug.LogError("MinionView received a null RuntimeCard.");

            return;
        }

        if (runtimeCard != card)
            shownHealth = shownAttack = null;

        runtimeCard = card;

        CardData data = card.Data;

        RefreshLocalization();
        if (runtimeCard == null)
            return;

        if (nameText == null)
            return;

        if (LocalizationManager.Instance != null)
        {
            nameText.text = LocalizationManager.Instance.GetCardName(runtimeCard.Data);
        }
        else
        {
            nameText.text = runtimeCard.Data.cardName;
        }

        if (data.artwork != null)
            artworkImage.sprite = data.artwork;

        if (frameBand != null)
            frameBand.color = card.Owner == PlayerSide.Player ? playerFrameColor : enemyFrameColor;

        attackText.gameObject.SetActive(false);
        healthText.gameObject.SetActive(false);
        SetRaceVisible(false);

        if (data is MonsterData monster)
        {
            attackText.gameObject.SetActive(true);
            healthText.gameObject.SetActive(true);

            RefreshStats();
        }

        if (data is ApostleData apostle)
        {
            attackText.gameObject.SetActive(true);
            healthText.gameObject.SetActive(true);
            SetRaceVisible(true);

            RefreshStats();
            raceImage.sprite = apostle.TypeIcon;
        }
        artifactOne.gameObject.SetActive(false);
        artifactTwo.gameObject.SetActive(false);
        artifactThree.gameObject.SetActive(false);

        Debug.Log($"MinionView created for {data.cardName}");
        RefreshArtifacts();
    }

    private void SetRaceVisible(bool visible)
    {
        raceImage.gameObject.SetActive(visible);

        if (raceBadge != null)
            raceBadge.SetActive(visible);
    }

    public void RefreshStats()
    {
        if (runtimeCard == null)
            return;

        if (attackText != null)
            attackText.text = runtimeCard.GetAttack().ToString();

        if (healthText != null)
            healthText.text = runtimeCard.CurrentHealth.ToString();

        AnnounceStatChanges();
    }

    private void AnnounceStatChanges()
    {
        int health = runtimeCard.CurrentHealth;
        int attack = runtimeCard.GetAttack();

        if (shownHealth.HasValue && shownHealth.Value != health)
        {
            BattleFeedback.HealthChanged(transform, health - shownHealth.Value);
            if (healthText != null)
                BattleFeedback.Punch(healthText.transform);
        }

        if (shownAttack.HasValue && shownAttack.Value != attack && attackText != null)
            BattleFeedback.Punch(attackText.transform);

        shownHealth = health;
        shownAttack = attack;
    }

    public void RefreshArtifacts()
    {
        ClearArtifactSlot(artifactOne);
        ClearArtifactSlot(artifactTwo);
        ClearArtifactSlot(artifactThree);

        if (runtimeCard == null)
            return;

        IReadOnlyList<RuntimeCard> artifacts = runtimeCard.EquippedArtifacts;

        for (int i = 0; i < artifacts.Count; i++)
        {
            RuntimeCard artifact = artifacts[i];

            if (artifact == null)
                continue;

            Image slot = GetArtifactSlot(i);

            if (slot == null)
                continue;

            slot.sprite = artifact.Data.artwork;
            slot.gameObject.SetActive(true);
        }
    }

    private void ClearArtifactSlot(Image slot)
    {
        if (slot == null)
            return;

        slot.sprite = null;
        slot.gameObject.SetActive(false);
    }

    private Image GetArtifactSlot(int index)
    {
        switch (index)
        {
            case 0:
                return artifactOne;

            case 1:
                return artifactTwo;

            case 2:
                return artifactThree;

            default:
                return null;
        }
    }

    private void RefreshLocalization()
    {
        if (runtimeCard == null)
            return;

        if (nameText == null)
            return;

        if (LocalizationManager.Instance != null)
        {
            nameText.text = LocalizationManager.Instance.GetCardName(runtimeCard.Data);
        }
        else
        {
            nameText.text = runtimeCard.Data.cardName;
        }
    }

    protected virtual void OnEnable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += RefreshLocalization;
        }
    }

    protected virtual void OnDisable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= RefreshLocalization;
        }
    }
}
