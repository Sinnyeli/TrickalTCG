
public static class DeckEditorSession
{
    public static string DeckID = null;

    public static bool CreatingNewDeck = true;


    public static void StartNewDeck()
    {
        DeckID = null;
        CreatingNewDeck = true;
    }


    public static void EditDeck(
        string deckID)
    {
        DeckID = deckID;
        CreatingNewDeck = false;
    }


    public static void Clear()
    {
        DeckID = null;
        CreatingNewDeck = true;
    }
}