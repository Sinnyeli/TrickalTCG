using System;
using System.Collections.Generic;

[Serializable]
public class DeckSaveData
{
    public string deckID;
    public string deckName;
    public string commanderID;

    public List<DeckCardEntry> cards =
        new List<DeckCardEntry>();
}

[Serializable]
public class DeckCardEntry
{
    public string cardID;
    public int count;

    public DeckCardEntry(
        string cardID,
        int count)
    {
        this.cardID = cardID;
        this.count = count;
    }
}