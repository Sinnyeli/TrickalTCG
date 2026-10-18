// Deck-building rules shared by the deck editor and online deck validation.
public static class DeckRules
{
    public const int MainDeckSize = 30;
    public const string JubeeCardID = "A_JUBEE";

    // Returns the maximum copies of a card allowed in a main deck; -1 means unlimited.
    public static int GetCopyLimit(CardData card)
    {
        if (card == null)
            return 0;

        if (card.CardID == JubeeCardID)
            return -1;

        if (card is SpellData)
            return 3;

        if (card is ApostleData || card is MonsterData || card is ArtifactData)
            return 2;

        // Unknown card types cannot be added to a deck.
        return 0;
    }
}
