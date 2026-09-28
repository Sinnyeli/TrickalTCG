using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public static class CardDataExporter
{
    [MenuItem("Tools/Export Card ID + Description")]
    public static void ExportCardData()
    {
        string[] guids = AssetDatabase.FindAssets("t:CardData");

        StringBuilder csv = new StringBuilder();

        // Header
        csv.AppendLine("ID,Description");

        int exportedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            CardData card =
                AssetDatabase.LoadAssetAtPath<CardData>(path);

            if (card == null)
                continue;

            string id = EscapeCSV(card.CardID);
            string description = EscapeCSV(card.description);

            csv.AppendLine($"{id},{description}");

            exportedCount++;
        }

        string exportPath = "Assets/CardDataExport.csv";

        File.WriteAllText(
            exportPath,
            csv.ToString(),
            new UTF8Encoding(true)
        );

        AssetDatabase.Refresh();

        Debug.Log(
            $"Exported {exportedCount} CardData assets to {exportPath}"
        );
    }

    private static string EscapeCSV(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "\"\"";

        // Escape quotation marks for CSV
        value = value.Replace("\"", "\"\"");

        return $"\"{value}\"";
    }
}