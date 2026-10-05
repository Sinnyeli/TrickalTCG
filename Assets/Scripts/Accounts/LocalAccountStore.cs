using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

// Local prototype. This account ID is the only identity; no separate user GUID/email.
public sealed class LocalAccountStore
{
    private const int Iterations = 600000;
    private readonly string root;
    [Serializable] private sealed class AccountRecord
    {
        public int version = 1;
        public string loginID;
        public string salt;
        public string passwordHash;
        public int iterations;
    }
    public LocalAccountStore(string rootPath) { root = rootPath; }
    public static bool TryNormalizeID(string input, out string canonical, out string error)
    {
        canonical = null; error = null;
        string id = (input ?? "").Trim().Normalize(NormalizationForm.FormC);
        if (id.Length < 1 || id.Length > 32) { error = "ID must be 1–32 characters."; return false; }
        foreach (char c in id)
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
            { error = "Use letters, numbers, underscore or hyphen for your ID."; return false; }
        canonical = id.ToLowerInvariant(); return true;
    }
    private string AccountPath(string canonical) => Path.Combine(root, "Accounts", "user_" + canonical + ".json");
    public string GetDeckFolder(string loginID)
    {
        if (!TryNormalizeID(loginID, out var canonical, out var error)) throw new ArgumentException(error);
        return Path.Combine(root, "Decks", "Users", "user_" + canonical);
    }
    public bool Register(string loginID, string password, out string error)
    {
        error = null;
        if (!TryNormalizeID(loginID, out var canonical, out error)) return false;
        if (string.IsNullOrEmpty(password) || password.Length > 128) { error = "Password must be 1–128 characters."; return false; }
        string path = AccountPath(canonical); bool created = false;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            if (File.Exists(path)) { error = "That ID already exists."; return false; }
            var salt = new byte[16]; using (var random = RandomNumberGenerator.Create()) random.GetBytes(salt);
            var record = new AccountRecord { loginID = loginID.Trim().Normalize(NormalizationForm.FormC),
                salt = Convert.ToBase64String(salt), passwordHash = Convert.ToBase64String(Derive(password, salt, Iterations)), iterations = Iterations };
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                created = true;
                using (var writer = new StreamWriter(file, new UTF8Encoding(false))) writer.Write(JsonUtility.ToJson(record, true));
            }
            LocalStorageSync.Flush(); return true;
        }
        catch (Exception exception)
        {
            if (created) { try { File.Delete(path); } catch (IOException) { } }
            Debug.LogError("Local account storage failed: " + exception.GetType().Name);
            error = !created && File.Exists(path) ? "That ID already exists." : "Could not save the account. Please try again.";
            return false;
        }
    }
    public bool Login(string loginID, string password, out string verifiedID, out string error)
    {
        verifiedID = null; error = "Incorrect ID or password.";
        if (!TryNormalizeID(loginID, out var canonical, out _) || string.IsNullOrEmpty(password) || password.Length > 128) return false;
        try
        {
            string path = AccountPath(canonical); if (!File.Exists(path)) return false;
            var record = JsonUtility.FromJson<AccountRecord>(File.ReadAllText(path));
            if (record == null || record.version != 1 || record.iterations < 100000 || record.iterations > 2000000 ||
                !TryNormalizeID(record.loginID, out var storedID, out _) || storedID != canonical) return false;
            byte[] salt = Convert.FromBase64String(record.salt), expected = Convert.FromBase64String(record.passwordHash);
            if (salt.Length != 16 || expected.Length != 32) return false;
            byte[] actual = Derive(password, salt, record.iterations); int difference = 0;
            for (int i = 0; i < actual.Length; i++) difference |= actual[i] ^ expected[i];
            if (difference != 0) return false;
            verifiedID = record.loginID; error = null; return true;
        }
        catch (Exception) { error = "Could not read this local account."; return false; }
    }
    private static byte[] Derive(string password, byte[] salt, int iterations)
    {
        using (var derive = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256)) return derive.GetBytes(32);
    }
}
