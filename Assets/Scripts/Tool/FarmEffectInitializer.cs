using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class FarmEffectInitializer
{
    private const string EffectPath = "Assets/Cards/Effects/Initialized/FX_FarmRandomVegetable.asset";

    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var cards = CardDataCsv.FindCards();
        var farms = cards.Where(c => c.CardID == "S_FARM").ToList();
        if (farms.Count != 1 || !(farms[0] is SpellData))
            throw new InvalidOperationException("Import exactly one S_FARM SpellData first.");
        // Includes token vegetables: this generates a unit, rather than drawing from a deck.
        var pool = cards.OfType<MonsterData>()
            .Where(c => c.cardRace == CardRace.Vegetable && c.health > 0)
            .OrderBy(c => c.CardID, StringComparer.Ordinal).ToList();
        if (pool.Count == 0)
            throw new InvalidOperationException("No living Vegetable Monster assets found. Import their race and stats first.");
        if (pool.Any(c => string.IsNullOrWhiteSpace(c.CardID)) ||
            pool.GroupBy(c => c.CardID).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Vegetable pool has missing or duplicate CardIDs. Correct these first.");
        var rows = CardDataCsv.ReadLocalizationRows();
        var existing = AssetDatabase.LoadMainAssetAtPath(EffectPath);
        if (existing != null && !(existing is SummonEffect))
            throw new InvalidOperationException("Effect type conflict at " + EffectPath);
        EnsureFolder("Assets/Cards/Effects/Initialized");
        var effect = existing as SummonEffect;
        if (effect == null)
        {
            effect = ScriptableObject.CreateInstance<SummonEffect>();
            AssetDatabase.CreateAsset(effect, EffectPath);
            Undo.RegisterCreatedObjectUndo(effect, "Create farming effect");
        }
        Undo.RecordObject(effect, "Configure farming effect");
        var serialized = new SerializedObject(effect);
        serialized.FindProperty("targetType").enumValueIndex = (int)EffectTargetType.None;
        serialized.FindProperty("targetFilter").enumValueIndex = (int)EffectTargetFilter.None;
        serialized.FindProperty("amount").intValue = 1;
        serialized.FindProperty("selectionType").enumValueIndex = (int)SummonSelectionType.Random;
        serialized.FindProperty("summonSideMode").enumValueIndex = (int)SummonSideMode.SourceOwner;
        var list = serialized.FindProperty("summonPool");
        list.arraySize = pool.Count;
        for (int i = 0; i < pool.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = pool[i];
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(effect);

        var farm = farms[0];
        Undo.RecordObject(farm, "Assign farming effect");
        serialized = new SerializedObject(farm);
        list = serialized.FindProperty("spellEffects");
        list.arraySize = 1;
        list.GetArrayElementAtIndex(0).objectReferenceValue = effect;
        const string english = "Summon a random Vegetable Monster.";
        const string korean = "무작위 채소 몬스터 1개를 소환합니다.";
        serialized.FindProperty("description").stringValue = english;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(farm);

        string key = "S_FARM_TEXT";
        var row = rows.Skip(1).FirstOrDefault(r => r[0] == key);
        if (row == null) { row = new List<string> { key, english, korean }; rows.Add(row); }
        else { row[1] = english; row[2] = korean; }
        var csv = new StringBuilder();
        foreach (var entry in rows) CardDataCsv.AppendRow(csv, entry);
        File.WriteAllText(CardDataCsv.LocalizationPath, csv.ToString(), new UTF8Encoding(true));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("S_FARM initialized with " + pool.Count + " Vegetable Monsters. Rerun after adding vegetables.");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
