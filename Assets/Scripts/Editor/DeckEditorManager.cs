using UnityEngine;

public class DeckEditorManager : MonoBehaviour
{
    [Header("Card Database")]
    [SerializeField]
    private CardDatabase cardDatabase;

    [Header("Card Library")]
    [SerializeField]
    private Transform cardLibraryContent;

    [SerializeField]
    private DeckEditorCardView cardViewPrefab;

    private void Start()
    {
        LoadCardLibrary();
    }

    private void LoadCardLibrary()
    {
        if (cardDatabase == null)
        {
            Debug.LogError(
                "DeckEditorManager: CardDatabase is not assigned."
            );
            return;
        }

        if (cardLibraryContent == null)
        {
            Debug.LogError(
                "DeckEditorManager: Card Library Content is not assigned."
            );
            return;
        }

        if (cardViewPrefab == null)
        {
            Debug.LogError(
                "DeckEditorManager: Card View Prefab is not assigned."
            );
            return;
        }

        foreach (CardData card in cardDatabase.AllCards)
        {
            if (card == null)
                continue;

            DeckEditorCardView view =
                Instantiate(
                    cardViewPrefab,
                    cardLibraryContent
                );

            view.SetCard(card);
        }

        Debug.Log(
            $"Displayed {cardDatabase.AllCards.Count} cards."
        );
    }
}