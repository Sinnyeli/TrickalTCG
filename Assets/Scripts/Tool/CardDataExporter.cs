using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CardDataExporter
{
    internal static readonly string[] Headers =
    {
        "Type", "ID", "Name", "Description", "Cost", "Attack", "Health",
        "Race", "Keywords", "Collectible", "UnlimitedCopies",
        "AttackBonus", "HealthBonus", "GrantedKeywords", "MaxEquipment",
        "NameEnglish", "NameKorean", "DescriptionEnglish", "DescriptionKorean"
    };

    [MenuItem("Tools/Cards/Export Card Data CSV")]
    public static void ExportCardData()
    {
        string path = EditorUtility.SaveFilePanel(
            "Export Card Data", Application.dataPath, "CardDataExport", "csv");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            List<CardData> cards = CardDataCsv.FindCards()
                .OrderBy(card => card.CardID, StringComparer.Ordinal).ToList();
            var translations = CardDataCsv.ReadLocalization();
            var csv = new StringBuilder();
            CardDataCsv.AppendRow(csv, Headers);
            foreach (CardData card in cards)
            {
                MinionData minion = card as MinionData;
                ArtifactData artifact = card as ArtifactData;
                ApostleData apostle = card as ApostleData;
                CardDataCsv.AppendRow(csv, new[]
                {
                    card.GetType().Name.Replace("Data", ""), card.CardID,
                    card.cardName, card.description, CardDataCsv.Number(card.manaCost),
                    minion == null ? "" : CardDataCsv.Number(minion.attack),
                    minion == null ? "" : CardDataCsv.Number(minion.health),
                    minion == null || minion.cardRace == CardRace.Unspecified ? "" : minion.cardRace.ToString(),
                    minion == null ? "" : string.Join(";", minion.Keywords.Select(k => k.ToString()).ToArray()),
                    card.Collectible ? "true" : "false",
                    card.UnlimitedCopies ? "true" : "false",
                    artifact == null ? "" : CardDataCsv.Number(artifact.AttackBonus),
                    artifact == null ? "" : CardDataCsv.Number(artifact.HealthBonus),
                    artifact == null ? "" : string.Join(";", Enum.GetValues(typeof(CardKeyword))
                        .Cast<CardKeyword>().Where(artifact.GrantsKeyword).Select(k => k.ToString()).ToArray()),
                    apostle == null ? "" : CardDataCsv.Number(apostle.MaxEquipment),
                    CardDataCsv.Translation(translations, card.CardID + "_NAME", 1),
                    CardDataCsv.Translation(translations, card.CardID + "_NAME", 2),
                    CardDataCsv.Translation(translations, card.CardID + "_TEXT", 1),
                    CardDataCsv.Translation(translations, card.CardID + "_TEXT", 2)
                });
            }
            File.WriteAllText(path, csv.ToString(), new UTF8Encoding(true));
            AssetDatabase.Refresh();
            Debug.Log($"Exported {cards.Count} cards to {path}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Card CSV export stopped: {exception.Message}");
            EditorUtility.DisplayDialog("Card CSV export stopped", exception.Message, "OK");
        }
    }
}

internal static class CardDataCsv
{
    internal const string LocalizationPath = "Assets/Localization/Localization.csv";

    internal static List<List<string>> ReadLocalizationRows()
    {
        if (!File.Exists(LocalizationPath))
            throw new FileNotFoundException("Card localization CSV is missing", LocalizationPath);
        var rows = Parse(File.ReadAllText(LocalizationPath, Encoding.UTF8).TrimStart('\uFEFF'));
        if (rows.Count == 0 || !rows[0].SequenceEqual(new[] { "Key", "English", "Korean" }))
            throw new FormatException("Localization.csv must begin with Key,English,Korean.");
        for (int i = 1; i < rows.Count; i++)
        {
            if (rows[i].Count != 3)
                throw new FormatException($"Localization.csv row {i + 1} must have three columns.");
            rows[i][0] = rows[i][0].Trim();
        }
        return rows;
    }

    internal static Dictionary<string, List<string>> ReadLocalization()
    {
        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var row in ReadLocalizationRows().Skip(1))
            if (!string.IsNullOrWhiteSpace(row[0]))
            {
                if (result.ContainsKey(row[0]))
                    throw new FormatException($"Duplicate localization key: {row[0]}.");
                result.Add(row[0], row);
            }
        return result;
    }

    internal static string Translation(Dictionary<string, List<string>> rows, string key, int language)
    {
        return rows.TryGetValue(key, out var row) ? row[language] : "";
    }

    internal static List<CardData> FindCards()
    {
        var cards = new List<CardData>();
        foreach (string guid in AssetDatabase.FindAssets("t:CardData"))
        {
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(guid));
            if (card != null) cards.Add(card);
        }
        return cards;
    }

    internal static string Number(int value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);

    internal static void AppendRow(StringBuilder builder, IEnumerable<string> fields)
    {
        builder.AppendLine(string.Join(",", fields.Select(value =>
            "\"" + (value ?? "").Replace("\"", "\"\"") + "\"").ToArray()));
    }

    internal static List<List<string>> Parse(string text) => CsvText.Parse(text);
}
