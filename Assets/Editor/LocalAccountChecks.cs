using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class LocalAccountChecks
{
    [MenuItem("Tools/Accounts/Run Local Account Checks")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        string root = Path.Combine(Path.GetTempPath(), "TrickalAccountChecks_" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new LocalAccountStore(root);
            Check(store.Register("Alice", "secret", out var error), error);
            Check(!store.Register(" ALICE ", "other", out _), "Case/whitespace duplicate ID was accepted.");
            Check(!store.Login("Alice", "wrong", out _, out _), "Wrong password was accepted.");
            Check(store.Login("alice", "secret", out var verified, out _), "Valid login failed."); Check(verified == "Alice", "Display ID changed.");
            Check(store.Register("Bob", "different", out error), error);
            Check(!store.Login("Bob", "secret", out _, out _), "One account accepted another account's password.");
            Check(store.Login("Bob", "different", out _, out _), "Second account login failed.");
            string alice = store.GetDeckFolder("Alice"), bob = store.GetDeckFolder("Bob"); Check(alice != bob, "Account folders collide.");
            Directory.CreateDirectory(alice); Directory.CreateDirectory(bob);
            File.WriteAllText(Path.Combine(alice, "deck1.json"), "Alice deck");
            Check(Directory.GetFiles(bob, "*.json").Length == 0, "Alice deck leaked into Bob folder.");
            Check(!store.Register("../Alice", "password", out _), "Path traversal ID was accepted.");
            Check(!store.Login("Nobody", "secret", out _, out _), "Unknown ID was accepted.");
            Check(!File.ReadAllText(Path.Combine(root, "Accounts", "user_alice.json")).Contains("secret"), "Password was stored as plaintext.");
            if (!LocalAccountSession.IsLoggedIn)
            {
                bool refused = false; try { AccountDeckStorage.GetFolder(); } catch (InvalidOperationException) { refused = true; }
                Check(refused, "Logged-out deck storage was allowed.");
            }
            Debug.Log("Local account checks passed: duplicate IDs, login, password isolation, deck isolation, invalid IDs and password storage.");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    private static void Check(bool result, string message) { if (!result) throw new InvalidOperationException(message); }
}
