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

    public bool PlayCard(RuntimeCard card)
    {
        if (card == null)
        {
            Debug.LogWarning("Tried to play a null card.");
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
            Debug.Log("Battlefield is full!");
            return false;
        }

        // Only Monsters and Apostles can become minions.
        if (!(card.Data is MonsterData) &&
            !(card.Data is ApostleData))
        {
            Debug.LogWarning(
                $"{card.Data.cardName} cannot be placed on the battlefield."
            );

            return false;
        }


        // Add to battlefield list.
        minions.Add(card);

        // Change logical zone.
        card.ChangeZone(CardZone.Field);
        card.InitializeCombatStats();
        // Create visual representation.
        CreateMinionView(card);

        //Debug.Log($"{card.Data.cardName} entered the {side} battlefield.");

        GameManager.Instance.EffectManager.ResolveBattlecry(card);

        return true;
    }

private void CreateMinionView(RuntimeCard card)
{
    GameObject minionObject =
        Instantiate(minionPrefab, minionContainer);

    MinionView minionView =
        minionObject.GetComponent<MinionView>();

    if (minionView == null)
    {
        Debug.LogError(
            "Minion prefab does not have a MinionView!"
        );

        Destroy(minionObject);
        return;
    }

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
        GameManager.Instance.EffectManager.ResolveDeathrattle(card);
        minions.Remove(card);

        card.ChangeZone(CardZone.Graveyard);

        RemoveMinionView(card);

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

}