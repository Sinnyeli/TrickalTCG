using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewDeck",
    menuName = "TCG/Deck"
)]
public class DeckData : ScriptableObject
{
    public ApostleData commander;

    public List<CardData> cards = new List<CardData>();
}