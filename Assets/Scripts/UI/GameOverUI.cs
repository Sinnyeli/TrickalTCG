using UnityEngine;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;


    public void Show(PlayerSide winner)
    {
        gameObject.SetActive(true);

        if (winner == PlayerSide.Player)
        {
            resultText.text = "VICTORY";
        }
        else
        {
            resultText.text = "DEFEAT";
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}