using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleMenuManager : MonoBehaviour
{
    // =========================================================
    // DECK EDIT
    // =========================================================

    public void OpenDeckEditor()
    {
        if (!LocalAccountSession.RequireLogin()) return;
        SceneManager.LoadScene(
            "DeckLoader"
        );
    }
       public void OpenDeckSelector()
    {
        if (!LocalAccountSession.RequireLogin()) return;
        SceneManager.LoadScene(
            "DeckSelector"
        );
    }
    public void Logout() => LocalAccountSession.LogoutAndOpenLogin();
}