using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenuManager : MonoBehaviour
{
    // =========================================================
    // DECK EDIT
    // =========================================================

    public void OpenDeckEditor()
    {
        SceneManager.LoadScene(
            "DeckLoader"
        );
    }
       public void OpenDeckSelector()
    {
        SceneManager.LoadScene(
            "DeckSelector"
        );
    }
}