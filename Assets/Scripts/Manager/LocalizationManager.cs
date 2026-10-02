using System;
using System.Collections.Generic;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance
    {
        get;
        private set;
    }


    [Header("Localization")]
    [SerializeField]
    private TextAsset localizationCSV;


    private Dictionary<string, LocalizationEntry>
        entries =
            new Dictionary<string, LocalizationEntry>();


    private GameLanguage currentLanguage =
        GameLanguage.English;


    public GameLanguage CurrentLanguage =>
        currentLanguage;


    public event Action OnLanguageChanged;


    private void Awake()
    {
        // =========================================
        // SINGLETON
        // =========================================

        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(
            gameObject
        );


        // =========================================
        // LOAD LANGUAGE SETTING
        // =========================================

        int savedLanguage =
            PlayerPrefs.GetInt(
                "GameLanguage",
                (int)GameLanguage.English
            );

        currentLanguage =
            (GameLanguage)savedLanguage;


        // =========================================
        // LOAD CSV
        // =========================================

        LoadLocalizationCSV();
    }


    // =========================================================
    // LOAD CSV
    // =========================================================

    private void LoadLocalizationCSV()
    {
        entries.Clear();
        if (localizationCSV == null)
        {
            Debug.LogError("Localization CSV is not assigned.");
            return;
        }
        try
        {
            var rows = CsvText.Parse(localizationCSV.text);
            if (rows.Count == 0 || rows[0].Count != 3 ||
                rows[0][0] != "Key" || rows[0][1] != "English" || rows[0][2] != "Korean")
                throw new FormatException("Localization CSV must begin with Key,English,Korean.");

            var loaded = new Dictionary<string, LocalizationEntry>(StringComparer.Ordinal);
            for (int i = 1; i < rows.Count; i++)
            {
                var values = rows[i];
                if (values.Count != 3)
                    throw new FormatException($"Localization row {i + 1} must have three columns.");
                string key = values[0].Trim();
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (loaded.ContainsKey(key))
                    throw new FormatException($"Duplicate localization key: {key}.");
                loaded.Add(key, new LocalizationEntry
                {
                    Key = key, English = values[1], Korean = values[2]
                });
            }
            entries = loaded;
            Debug.Log($"Loaded {entries.Count} localization entries.");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Localization CSV could not be loaded: {exception.Message}");
        }
    }

    // =========================================================
    // GET TEXT
    // =========================================================

    public string Get(
        string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return "";


        if (entries.TryGetValue(
                key,
                out LocalizationEntry entry))
        {
            return entry.GetText(
                currentLanguage
            );
        }


        Debug.LogWarning(
            $"Missing localization key: {key}"
        );


        // Returning the key makes missing
        // translations obvious during development.
        return $"[{key}]";
    }


    // =========================================================
    // SET LANGUAGE
    // =========================================================

    public void SetLanguage(
        GameLanguage language)
    {
        if (currentLanguage == language)
            return;


        currentLanguage =
            language;


        PlayerPrefs.SetInt(
            "GameLanguage",
            (int)language
        );

        PlayerPrefs.Save();


        OnLanguageChanged?.Invoke();


        Debug.Log(
            $"Language changed to: {language}"
        );
    }


    public void SetEnglish()
    {
        SetLanguage(
            GameLanguage.English
        );
    }


    public void SetKorean()
    {
        SetLanguage(
            GameLanguage.Korean
        );
    }


    public string GetCardName(CardData card)
    {
        if (card == null)
            return "";
        string key = card.CardID + "_NAME";
        if (entries.TryGetValue(key, out LocalizationEntry entry))
        {
            string localized = entry.GetText(currentLanguage);
            if (!string.IsNullOrWhiteSpace(localized)) return localized;
        }
        return card.cardName;
    }

    public string GetCardText(CardData card)
    {
        if (card == null)
            return "";
        string key = card.CardID + "_TEXT";
        if (entries.TryGetValue(key, out LocalizationEntry entry))
        {
            string localized = entry.GetText(currentLanguage);
            if (!string.IsNullOrWhiteSpace(localized)) return localized;
        }
        return card.description;
    }
}
