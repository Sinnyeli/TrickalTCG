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

        // Change logical zone.
        card.ChangeZone(CardZone.Field);

        // Add to battlefield list.
        minions.Add(card);

        // Create visual representation.
        CreateMinionView(card);

        Debug.Log(
            $"{card.Data.cardName} entered the battlefield."
        );

        return true;
    }

    private void CreateMinionView(RuntimeCard card)
    {
        GameObject minionObject = Instantiate(
            minionPrefab,
            minionContainer
        );

        MinionView minionView =
            minionObject.GetComponent<MinionView>();

        if (minionView != null)
        {
            minionView.SetMinion(card);
        }
        else
        {
            Debug.LogError(
                "Minion Prefab is missing MinionView component!"
            );
        }
    }

    public bool PlayCardAtPosition(
    RuntimeCard card,
    Vector2 screenPosition)
{
    if (card == null)
        return false;

    if (card.Zone != CardZone.Hand)
        return false;

    if (minions.Count >= maxMinions)
    {
        Debug.Log("Battlefield is full!");
        return false;
    }

    RectTransform rect =
        minionContainer as RectTransform;

    Vector2 localPosition;

    RectTransformUtility.ScreenPointToLocalPointInRectangle(
        rect,
        screenPosition,
        null,
        out localPosition
    );

    int insertionIndex =
        battlefieldLayout.GetInsertionIndex(
            localPosition.x
        );

    // Remove from hand
    bool removed =
        GameManager.Instance.HandManager
            .RemoveCardFromHand(card);

    if (!removed)
        return false;

    // Add to battlefield
    card.ChangeZone(CardZone.Field);

    minions.Insert(
        insertionIndex,
        card
    );

    CreateMinionView(card);

    battlefieldLayout.RefreshLayout();

    return true;
}
}