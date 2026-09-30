using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>Updates cards by ID, preserving artwork and effect references.</summary>
public static class CardDataImporter
{
    private sealed class Row
    {
        public string Type, ID, Name, Description;
        public int Cost, Attack, Health, AttackBonus, HealthBonus, MaxEquipment;
        public CardRace Race;
        public List<CardKeyword> Keywords, GrantedKeywords;
        public bool Collectible, UnlimitedCopies;
        public CardData Existing;
    }

    [MenuItem("Tools/Cards/Import Card Data CSV")]
    public static void ImportCardData()
    {
        string path = EditorUtility.OpenFilePanel("Import Card Data", Application.dataPath, "csv");
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            List<Row> rows = ReadAndValidate(path);
            int created = rows.Count(row => row.Existing == null);
            if (!EditorUtility.DisplayDialog("Import Card Data",
                $"Validated {rows.Count} rows: create {created}, update {rows.Count - created}.\n\nArtwork, effect references, and existing asset paths will be preserved.",
                "Import", "Cancel")) return;

            var newCards = new List<CardData>();
            foreach (Row row in rows)
            {
                CardData card = Apply(row);
                if (row.Existing == null) newCards.Add(card);
            }
            RegisterNewCards(newCards);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Imported {rows.Count} cards ({created} new) from {path}");
        }
        catch (Exception exception)
        {
            Debug.LogError($"Card CSV import stopped: {exception.Message}");
            EditorUtility.DisplayDialog("Card CSV import stopped", exception.Message, "OK");
        }
    }

    private static List<Row> ReadAndValidate(string path)
    {
        string content = File.ReadAllText(path, Encoding.UTF8).TrimStart('\uFEFF');
        List<List<string>> table = CardDataCsv.Parse(content);
        if (table.Count == 0) throw new FormatException("CSV is empty.");
        table[0][0] = table[0][0].TrimStart('\uFEFF');
        if (!table[0].SequenceEqual(CardDataExporter.Headers))
            throw new FormatException("Header mismatch. Re-export with Tools > Cards > Export Card Data CSV. The old ID,Description file is not importable.");

        var existing = new Dictionary<string, CardData>(StringComparer.Ordinal);
        foreach (CardData card in CardDataCsv.FindCards())
        {
            if (string.IsNullOrWhiteSpace(card.CardID))
                throw new FormatException($"Existing card {AssetDatabase.GetAssetPath(card)} has no ID.");
            if (existing.ContainsKey(card.CardID))
                throw new FormatException($"Duplicate existing CardID: {card.CardID}.");
            existing.Add(card.CardID, card);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var rows = new List<Row>();
        for (int index = 1; index < table.Count; index++)
        {
            List<string> cells = table[index];
            int line = index + 1;
            if (cells.Count != CardDataExporter.Headers.Length)
                throw new FormatException($"Row {line}: expected {CardDataExporter.Headers.Length} columns, found {cells.Count}.");
            string id = cells[1].Trim();
            if (!Regex.IsMatch(id, "^[A-Za-z0-9_-]+$"))
                throw new FormatException($"Row {line}: CardID '{id}' must contain only ASCII letters, numbers, _ or -.");
            if (!seen.Add(id)) throw new FormatException($"Row {line}: duplicate CardID '{id}'.");
            string type = cells[0].Trim();
            if (type != "Apostle" && type != "Monster" && type != "Spell" && type != "Artifact")
                throw new FormatException($"Row {line}: unknown Type '{type}'.");
            existing.TryGetValue(id, out CardData card);
            if (card != null && card.GetType().Name != type + "Data")
                throw new FormatException($"Row {line}: {id} is a {card.GetType().Name}; changing type would break references.");
            if (string.IsNullOrWhiteSpace(cells[2]))
                throw new FormatException($"Row {line}: Name is required.");

            var row = new Row
            {
                Type = type, ID = id, Name = cells[2], Description = cells[3],
                Cost = ParseInt(cells[4], line, "Cost"),
                Collectible = ParseBool(cells[9], line, "Collectible"),
                UnlimitedCopies = ParseBool(cells[10], line, "UnlimitedCopies"), Existing = card
            };
            if (row.Cost < 0) throw new FormatException($"Row {line}: Cost cannot be negative.");
            if (type == "Apostle" || type == "Monster")
            {
                row.Attack = ParseInt(cells[5], line, "Attack");
                row.Health = ParseInt(cells[6], line, "Health");
                if (row.Health <= 0) throw new FormatException($"Row {line}: Health must be positive.");
                row.Race = string.IsNullOrWhiteSpace(cells[7]) ? CardRace.Unspecified : ParseEnum<CardRace>(cells[7], line, "Race");
                row.Keywords = ParseKeywords(cells[8], line, "Keywords");
                if (type == "Apostle")
                {
                    row.MaxEquipment = ParseInt(cells[14], line, "MaxEquipment");
                    if (row.MaxEquipment < 0) throw new FormatException($"Row {line}: MaxEquipment cannot be negative.");
                }
            }
            if (type == "Artifact")
            {
                row.AttackBonus = ParseInt(cells[11], line, "AttackBonus");
                row.HealthBonus = ParseInt(cells[12], line, "HealthBonus");
                row.GrantedKeywords = ParseKeywords(cells[13], line, "GrantedKeywords");
            }
            rows.Add(row);
        }
        if (rows.Count == 0) throw new FormatException("CSV has no card rows.");
        return rows;
    }

    private static int ParseInt(string text, int row, string column)
    {
        if (!int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
            throw new FormatException($"Row {row}: {column} must be an integer.");
        return value;
    }

    private static bool ParseBool(string text, int row, string column)
    {
        if (!bool.TryParse(text.Trim(), out bool value))
            throw new FormatException($"Row {row}: {column} must be true or false.");
        return value;
    }

    private static T ParseEnum<T>(string text, int row, string column) where T : struct
    {
        if (!Enum.TryParse(text.Trim(), true, out T value) || !Enum.IsDefined(typeof(T), value))
            throw new FormatException($"Row {row}: unknown {column} '{text}'.");
        return value;
    }

    private static List<CardKeyword> ParseKeywords(string text, int row, string column)
    {
        var result = new List<CardKeyword>();
        if (string.IsNullOrWhiteSpace(text)) return result;
        foreach (string part in text.Split(';'))
        {
            CardKeyword keyword = ParseEnum<CardKeyword>(part, row, column);
            if (result.Contains(keyword)) throw new FormatException($"Row {row}: duplicate {column} '{keyword}'.");
            result.Add(keyword);
        }
        return result;
    }

    private static CardData Apply(Row row)
    {
        CardData card = row.Existing;
        if (card == null)
        {
            switch (row.Type)
            {
                case "Apostle": card = ScriptableObject.CreateInstance<ApostleData>(); break;
                case "Monster": card = ScriptableObject.CreateInstance<MonsterData>(); break;
                case "Spell": card = ScriptableObject.CreateInstance<SpellData>(); break;
                default: card = ScriptableObject.CreateInstance<ArtifactData>(); break;
            }
            string folder = "Assets/Cards/" + row.Type;
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Cards", row.Type);
            AssetDatabase.CreateAsset(card, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + row.ID + ".asset"));
            Undo.RegisterCreatedObjectUndo(card, "Import Card CSV");
        }
        else Undo.RecordObject(card, "Import Card CSV");

        SerializedObject serialized = new SerializedObject(card);
        serialized.FindProperty("cardID").stringValue = row.ID;
        serialized.FindProperty("cardName").stringValue = row.Name;
        serialized.FindProperty("description").stringValue = row.Description;
        serialized.FindProperty("manaCost").intValue = row.Cost;
        serialized.FindProperty("collectible").boolValue = row.Collectible;
        serialized.FindProperty("unlimitedCopies").boolValue = row.UnlimitedCopies;
        if (row.Type == "Apostle" || row.Type == "Monster")
        {
            serialized.FindProperty("attack").intValue = row.Attack;
            serialized.FindProperty("health").intValue = row.Health;
            serialized.FindProperty("cardRace").enumValueIndex = (int)row.Race;
            SetKeywords(serialized.FindProperty("keywords"), row.Keywords);
            if (row.Type == "Apostle") serialized.FindProperty("maxEquipment").intValue = row.MaxEquipment;
        }
        if (row.Type == "Artifact")
        {
            serialized.FindProperty("attackBonus").intValue = row.AttackBonus;
            serialized.FindProperty("healthBonus").intValue = row.HealthBonus;
            SetKeywords(serialized.FindProperty("grantedKeywords"), row.GrantedKeywords);
        }
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(card);
        return card;
    }

    private static void RegisterNewCards(List<CardData> cards)
    {
        if (cards.Count == 0) return;
        CardDatabase database = AssetDatabase.LoadAssetAtPath<CardDatabase>(
            "Assets/Cards/Database/CardDatabase.asset");
        if (database == null)
        {
            Debug.LogWarning("New cards were created, but CardDatabase.asset was not found. Add them to the database manually.");
            return;
        }
        Undo.RecordObject(database, "Import Card CSV");
        SerializedObject serialized = new SerializedObject(database);
        SerializedProperty list = serialized.FindProperty("allCards");
        foreach (CardData card in cards)
        {
            bool present = false;
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue != card) continue;
                present = true;
                break;
            }
            if (present) continue;
            int index = list.arraySize;
            list.arraySize++;
            list.GetArrayElementAtIndex(index).objectReferenceValue = card;
        }
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(database);
    }

    private static void SetKeywords(SerializedProperty property, List<CardKeyword> keywords)
    {
        property.arraySize = keywords.Count;
        for (int i = 0; i < keywords.Count; i++)
            property.GetArrayElementAtIndex(i).enumValueIndex = (int)keywords[i];
    }
}
