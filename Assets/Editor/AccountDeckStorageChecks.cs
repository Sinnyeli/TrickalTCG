using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Called by the existing LocalAccountChecks menu. Exercises the real deck-list/delete manager.
public static class AccountDeckStorageChecks
{
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || LocalAccountSession.IsLoggedIn)
        { Debug.LogWarning("Run deck isolation checks outside Play Mode while logged out."); return; }
        string suffix = Guid.NewGuid().ToString("N").Substring(0, 12);
        string a = "deckcheck_a_" + suffix, b = "deckcheck_b_" + suffix;
        const string password = "Temporary-check-password";
        var store = LocalAccountSession.Store; bool createdA = false, createdB = false;
        GameObject testObject = null;
        try
        {
            createdA = store.Register(a, password, out var error); Check(createdA, error);
            createdB = store.Register(b, password, out error); Check(createdB, error);
            testObject = new GameObject("Temporary deck storage check") { hideFlags = HideFlags.HideAndDontSave };
            var manager = testObject.AddComponent<DeckLoaderManager>();
            var load = typeof(DeckLoaderManager).GetMethod("LoadAllDecks", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(load != null, "Deck-list loader was not found.");
            Check(LocalAccountSession.Login(a, password, out error), error);
            WriteFixture("fixture", a); string pathA = AccountDeckStorage.GetDeckPath("fixture");
            var decksA = (List<DeckSaveData>)load.Invoke(manager, null);
            Check(decksA.Count == 1 && decksA[0].deckName == a, "Account A did not load only its own deck.");
            DeckEditorSession.EditDeck("fixture"); BattleSession.CreateAIMatch("fixture", AIType.Jubee);
            Check(LocalAccountSession.Login(b, password, out error), error);
            Check(DeckEditorSession.CreatingNewDeck && !BattleSession.HasPlayerDeck(), "Selected decks survived switching accounts.");
            Check(((List<DeckSaveData>)load.Invoke(manager, null)).Count == 0, "Account B can see A's deck.");
            WriteFixture("fixture", b); string pathB = AccountDeckStorage.GetDeckPath("fixture");
            Check(pathA != pathB, "The same deck ID maps to a shared account path.");
            var decksB = (List<DeckSaveData>)load.Invoke(manager, null);
            Check(decksB.Count == 1 && decksB[0].deckName == b, "Account B did not load only its own deck.");
            Check(LocalAccountSession.Login(a, password, out error), error);
            manager.DeleteDeck("fixture");
            Check(!File.Exists(pathA) && File.Exists(pathB), "Deleting A's deck affected B's deck.");
            LocalAccountSession.Logout(); bool refused = false;
            try { AccountDeckStorage.GetFolder(); } catch (InvalidOperationException) { refused = true; }
            Check(refused, "Deck storage allowed access after logout.");
            Debug.Log("Deck-manager integration checks passed: isolated listing/deletion, same-ID decks, account switching and logout.");
        }
        finally
        {
            LocalAccountSession.Logout();
            if (testObject != null) UnityEngine.Object.DestroyImmediate(testObject);
            if (createdA) RemoveFixtureAccount(store, a);
            if (createdB) RemoveFixtureAccount(store, b);
        }
    }
    private static void WriteFixture(string deckID, string name)
    {
        Directory.CreateDirectory(AccountDeckStorage.GetFolder());
        var deck = new DeckSaveData { deckID = deckID, deckName = name, commanderID = "TEST_COMMANDER" };
        File.WriteAllText(AccountDeckStorage.GetDeckPath(deckID), JsonUtility.ToJson(deck));
    }
    private static void RemoveFixtureAccount(LocalAccountStore store, string id)
    {
        string folder = store.GetDeckFolder(id); if (Directory.Exists(folder)) Directory.Delete(folder, true);
        string path = Path.Combine(Application.persistentDataPath, "Accounts", "user_" + id + ".json");
        if (File.Exists(path)) File.Delete(path);
    }
    private static void Check(bool result, string message) { if (!result) throw new InvalidOperationException(message); }
}
