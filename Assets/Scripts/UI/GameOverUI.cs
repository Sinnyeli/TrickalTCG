using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;


    [SerializeField, Min(0f)] private float returnDelaySeconds = 3f;
    private float returnCountdown;
    private bool returning;

    private void Update()
    {
        if (!returning) return;
        returnCountdown -= Time.unscaledDeltaTime;
        if (returnCountdown <= 0f) ReturnToDeckSelector();
    }

    public void ReturnToDeckSelector()
    {
        if (!returning) return;
        returning = false;
        BattleSession.Clear();
        Time.timeScale = 1f;
        SceneManager.LoadScene("DeckSelector");
    }

    public void Show(PlayerSide winner)
    {
        gameObject.SetActive(true);
        returning = true;
        returnCountdown = returnDelaySeconds;

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
        returning = false;
        gameObject.SetActive(false);
    }
}