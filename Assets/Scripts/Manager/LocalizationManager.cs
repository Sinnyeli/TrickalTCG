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
            Debug.LogError(
                "Localization CSV is not assigned."
            );

            return;
        }


        string[] lines =
            localizationCSV.text.Split(
                new[] { "\r\n", "\n" },
                StringSplitOptions.RemoveEmptyEntries
            );


        // Skip header.
        for (int i = 1;
             i < lines.Length;
             i++)
        {
            string line =
                lines[i];


            string[] values =
                ParseCSVLine(line);


            if (values.Length < 3)
            {
                Debug.LogWarning(
                    $"Invalid localization row: {line}"
                );

                continue;
            }


            LocalizationEntry entry =
                new LocalizationEntry
                {
                    Key = values[0].Trim(),
                    English = values[1].Trim(),
                    Korean = values[2].Trim()
                };


            if (string.IsNullOrWhiteSpace(
                    entry.Key))
            {
                continue;
            }


            entries[entry.Key] =
                entry;
        }


        Debug.Log(
            $"Loaded {entries.Count} " +
            $"localization entries."
        );
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


    // =========================================================
    // BASIC CSV PARSER
    // =========================================================

    private string[] ParseCSVLine(
        string line)
    {
        List<string> values =
            new List<string>();

        bool insideQuotes = false;

        string currentValue = "";


        for (int i = 0;
             i < line.Length;
             i++)
        {
            char character =
                line[i];


            if (character == '"')
            {
                // "" inside a quoted value
                // represents a literal quote.
                if (insideQuotes &&
                    i + 1 < line.Length &&
                    line[i + 1] == '"')
                {
                    currentValue += '"';
                    i++;

                    continue;
                }


                insideQuotes =
                    !insideQuotes;

                continue;
            }


            if (character == ',' &&
                !insideQuotes)
            {
                values.Add(
                    currentValue
                );

                currentValue = "";

                continue;
            }


            currentValue +=
                character;
        }


        values.Add(
            currentValue
        );


        return values.ToArray();
    }

    public string GetCardName(CardData card)
    {
        if (card == null)
            return "";

        return Get(
            card.CardID + "_NAME"
        );
    }

    public string GetCardText(CardData card)
    {
        if (card == null)
            return "";

        return Get(
            card.CardID + "_TEXT"
        );
    }
}