using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LocalAccountSession
{
    public const string LoginScene = "Login";
    public static string LoginID { get; private set; }
    public static bool IsLoggedIn => !string.IsNullOrEmpty(LoginID);
    public static LocalAccountStore Store => new LocalAccountStore(Application.persistentDataPath);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset() { LoginID = null; DeckEditorSession.Clear(); BattleSession.Clear(); }
    public static bool Login(string id, string password, out string error)
    {
        if (!Store.Login(id, password, out var verifiedID, out error)) return false;
        DeckEditorSession.Clear(); BattleSession.Clear(); LoginID = verifiedID; return true;
    }
    public static void Logout() { LoginID = null; DeckEditorSession.Clear(); BattleSession.Clear(); }
    public static void LogoutAndOpenLogin() { Logout(); SceneManager.LoadScene(LoginScene); }
    public static bool RequireLogin()
    {
        if (IsLoggedIn) return true;
        if (SceneManager.GetActiveScene().name != LoginScene)
        {
            if (Application.CanStreamedLevelBeLoaded(LoginScene)) SceneManager.LoadScene(LoginScene);
            else Debug.LogError("Create/add the Login scene to Build Settings before accessing decks.");
        }
        return false;
    }
}

public static class AccountDeckStorage
{
    public static string GetFolder()
    {
        if (!LocalAccountSession.IsLoggedIn) throw new InvalidOperationException("Login is required to access saved decks.");
        return LocalAccountSession.Store.GetDeckFolder(LocalAccountSession.LoginID);
    }
    public static string GetDeckPath(string deckID)
    {
        if (string.IsNullOrEmpty(deckID) || deckID.Length > 128) throw new ArgumentException("Invalid deck ID.");
        foreach (char c in deckID) if (!char.IsLetterOrDigit(c) && c != '-' && c != '_') throw new ArgumentException("Invalid deck ID.");
        return Path.Combine(GetFolder(), deckID + ".json");
    }
}
