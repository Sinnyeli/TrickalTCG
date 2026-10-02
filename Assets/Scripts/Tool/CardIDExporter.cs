using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

public static class CardIDExporter
{
    [MenuItem("Tools/Cards/Export Card IDs TXT")]
    public static void ExportCardIDs()
    {
        string[] guids = AssetDatabase.FindAssets("t:CardData");

        StringBuilder output = new StringBuilder();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);

            if (card == null)
                continue;

            output.AppendLine(card.CardID);
        }

        string exportPath = "Assets/CardIDs.txt";

        File.WriteAllText(exportPath, output.ToString(), Encoding.UTF8);

        AssetDatabase.Refresh();

        Debug.Log($"Exported {guids.Length} Card IDs to {exportPath}");
    }
}