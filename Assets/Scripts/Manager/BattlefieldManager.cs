using System.Collections.Generic;
using UnityEngine;

public class BattlefieldManager : MonoBehaviour
{
    [Header("Field Settings")]
    [SerializeField] private int maxMinions = 6;

    [Header("UI")]
    [SerializeField] private Transform minionContainer;
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private BattlefieldLayout battlefieldLayout;
    
    [SerializeField]
    private PlayerSide side;
    
    [SerializeField]
    private GameManager gameManager;

    private List<RuntimeCard> minions = new List<RuntimeCard>();
    // This has the list of cards in field
    private Dictionary<RuntimeCard, MinionView> minionViews =
        new Dictionary<RuntimeCard, MinionView>();
    public IReadOnlyList<RuntimeCard> Minions => minions;


    private List<RuntimeCard> GetAllEquippedArtifacts()
    {
        List<RuntimeCard> artifacts = new List<RuntimeCard>();

        foreach (RuntimeCard minion in minions)
        {
            foreach (RuntimeCard artifact in minion.EquippedArtifacts)
            {
                if (artifact == null)
                    continue;

                artifacts.Add(artifact);
            }
        }

        return artifacts;
    }
  public bool PlayCard(RuntimeCard card)
{
    if (card == null)
    {
        Debug.LogWarning(
            "Tried to play a null card."
        );

        return false;
    }

    // Card must currently be in Hand.
    if (card.Zone != CardZone.Hand)
    {
        Debug.LogWarning(
            $"{card.Data.cardName} cannot be played. " +
            $"Current zone: {card.Zone}"
        );

        return false;
    }

    // Check field capacity.
    if (minions.Count >= maxMinions)
    {
        Debug.Log(
            "Battlefield is full!"
        );

        return false;
    }

    // Only Monsters and Apostles can become minions.
    if (!(card.Data is MonsterData) &&
        !(card.Data is ApostleData))
    {
        Debug.LogWarning(
            $"{card.Data.cardName} cannot be placed " +
            $"on the battlefield."
        );

        return false;
    }

    // =====================================================
    // COMMIT PLAY
    // =====================================================

    // Validation succeeded.
    // The card is now being played, so it leaves the hand
    // BEFORE any Battlecry can resolve.
    HandManager hand =
        GameManager.Instance.GetHandManager(
            card.Owner
        );

    if (hand != null)
    {
        hand.RemoveCardFromHand(card);
    }

    // Add to battlefield.
    minions.Add(card);

    // Change logical zone.
    card.ChangeZone(
        CardZone.Field
    );

    card.InitializeCombatStats();

    // Create visual representation.
    CreateMinionView(card);

    RefreshPassives();
    RefreshArtifactEffects();

    // Battlecry happens AFTER the card has left the hand.
    GameManager.Instance
        .EffectManager
        .ResolveBattlecry(card);

    return true;
}

    private void CreateMinionView(RuntimeCard card)
    {
        GameObject minionObject =
            Instantiate(minionPrefab, minionContainer);

        MinionView minionView =
            minionObject.GetComponent<MinionView>();

        minionView.SetMinion(card);

        minionViews[card] = minionView;
    }

    public void RefreshMinionView(RuntimeCard card)
    {
        if (card == null)
            return;

        if (!minionViews.TryGetValue(card, out MinionView view))
            return;

        if (view == null)
            return;

         view.RefreshStats();
         view.RefreshArtifacts();

         Debug.Log(
                $"Refreshing UI for {card.Data.cardName}: " +
                $"{card.GetAttack()}/{card.CurrentHealth}"
            );
    }   

    private void RemoveMinionView(RuntimeCard card)
    {
        if (!minionViews.TryGetValue(card, out MinionView view))
            return;

        if (view != null)
            Destroy(view.gameObject);

        minionViews.Remove(card);
    }


    public void RefreshAttackers(PlayerSide side)
    {
        foreach (RuntimeCard card in minions)
        {
            if (card.Owner == side)
            {
                card.ResetForTurn();
            }
        }
    }
    public bool RemoveCard(RuntimeCard card)
    {
        if (card == null)
            return false;

        if (!minions.Contains(card))
            return false;

        // Remove effects from artifacts equipped to this minion.
        foreach (RuntimeCard artifact in card.EquippedArtifacts)
        {
            if (artifact == null)
                continue;

            foreach (RuntimeCard minion in minions)
            {
                minion.RemoveModifiersFromSource(artifact);
            }
        }

        minions.Remove(card);
        card.ChangeZone(CardZone.Graveyard);

        RemoveMinionView(card);

        RefreshPassives();
        RefreshArtifactEffects();

        GameManager.Instance
            .EffectManager
            .ResolveDeathrattle(card);

        return true;
    }
    public void SetMinionSelected(RuntimeCard card, bool selected)
{
    if (card == null)
        return;

    if (!minionViews.TryGetValue(
        card,
        out MinionView view))
    {
        return;
    }

    if (view == null)
        return;

    view.SetSelected(selected);
    battlefieldLayout.RefreshLayout();
}
   public void RefreshPassives()
{
    // Remove all existing passive effects first.
    foreach (RuntimeCard minion in minions)
    {
        minion.RemovePassiveModifiers();
        minion.ClearPassiveBaseStatOverride();
    }

    // Recalculate all active passives.
    foreach (RuntimeCard source in minions)
    {
        if (!(source.Data is ApostleData))
            continue;

        if (source.IsSilenced)
            continue;

        CardEffect effect = source.Data.Passive;

        if (effect == null)
            continue;

        List<RuntimeCard> targets = new List<RuntimeCard>();

        foreach (RuntimeCard target in minions)
        {
            if (effect.MatchesTargetFilter(target))
            {
                targets.Add(target);
            }
        }

        effect.Resolve(source, targets);
    }
}
  public void RefreshArtifactEffects()
{
    // Remove existing artifact-effect modifiers.
    foreach (RuntimeCard minion in minions)
    {
        foreach (RuntimeCard artifact in GetAllEquippedArtifacts())
        {
            if (artifact == null)
                continue;

            minion.RemoveModifiersFromSource(artifact);
        }
    }

    // Reapply artifact effects.
    foreach (RuntimeCard carrier in minions)
    {
        foreach (RuntimeCard artifact in carrier.EquippedArtifacts)
        {
            if (artifact == null)
                continue;

            if (!(artifact.Data is ArtifactData artifactData))
                continue;

            if (artifactData.ArtifactEffect == null)
                continue;

            GameManager.Instance.EffectManager.ResolveArtifactEffect(
                artifact,
                artifactData.ArtifactEffect
            );
        }
    }

    // Refresh UI.
    foreach (RuntimeCard minion in minions)
    {
        RefreshMinionView(minion);
    }
}

}

