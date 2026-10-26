using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OpponentCardView : CardviewBase
{
    // Drawn over the face so the opponent's hand cards stay hidden.
    public GameObject cardBack;

    public void SetFaceDown(bool faceDown)
    {
        if (cardBack == null)
            return;

        cardBack.SetActive(faceDown);
        cardBack.transform.SetAsLastSibling();
    }
}
