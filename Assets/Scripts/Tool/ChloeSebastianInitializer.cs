using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ChloeSebastianInitializer
{
    private const string Folder = "Assets/Cards/Effects/Initialized";

    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var cards = CardDataCsv.FindCards();
        CardData chloe = Require(cards, "A_CHLOE", typeof(ApostleData));
        CardData sebastian = Require(cards, "M_SEBASTIAN", typeof(MonsterData));
        var rows = CardDataCsv.ReadLocalizationRows();
        // Validate asset types before making changes.
        CheckType<SummonEffect>("FX_SummonSebastian");
        CheckType<DamageRedirectEffect>("FX_SebastianProtectChloe");
        EnsureFolder(Folder);
        var summon = GetEffect<SummonEffect>("FX_SummonSebastian");
        Edit(summon, s => {
            s.FindProperty("targetType").enumValueIndex = (int)EffectTargetType.None;
            s.FindProperty("targetFilter").enumValueIndex = (int)EffectTargetFilter.None;
            s.FindProperty("amount").intValue = 1;
            s.FindProperty("selectionType").enumValueIndex = (int)SummonSelectionType.Selected;
            s.FindProperty("summonSideMode").enumValueIndex = (int)SummonSideMode.SourceOwner;
            var pool = s.FindProperty("summonPool");
            pool.arraySize = 1;
            pool.GetArrayElementAtIndex(0).objectReferenceValue = sebastian;
        });
        var redirect = GetEffect<DamageRedirectEffect>("FX_SebastianProtectChloe");
        Edit(redirect, s => {
            s.FindProperty("targetType").enumValueIndex = (int)EffectTargetType.None;
            s.FindProperty("targetFilter").enumValueIndex = (int)EffectTargetFilter.None;
            s.FindProperty("protectedCardID").stringValue = "A_CHLOE";
        });
        Edit(chloe, s => s.FindProperty("battlecry").objectReferenceValue = summon);
        // Keep unrelated passives; append this one only if not already assigned.
        Edit(sebastian, s => {
            if (s.FindProperty("passive").objectReferenceValue == redirect) return;
            var list = s.FindProperty("additionalPassives");
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == redirect) return;
            int index = list.arraySize++;
            list.GetArrayElementAtIndex(index).objectReferenceValue = redirect;
        });
        SetText(chloe, rows, "Battlecry: Summon Sebastian.", "등장: 세바스티안 소환.");
        var keywords = ((MinionData)sebastian).Keywords;
        bool taunt = keywords != null && keywords.Contains(CardKeyword.Taunt);
        SetText(sebastian, rows,
            (taunt ? "Taunt. " : "") + "Takes damage instead of friendly Chloe.",
            (taunt ? "도발. " : "") + "아군 클로에가 받을 피해를 대신 받습니다.");
        var csv = new StringBuilder();
        foreach (var row in rows) CardDataCsv.AppendRow(csv, row);
        File.WriteAllText(CardDataCsv.LocalizationPath, csv.ToString(), new UTF8Encoding(true));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Chloe summons Sebastian; Sebastian redirects damage from friendly Chloe.");
    }

    private static CardData Require(List<CardData> cards, string id, Type type)
    {
        var matches = cards.Where(c => c.CardID == id).ToList();
        if (matches.Count != 1 || !type.IsInstanceOfType(matches[0]))
            throw new InvalidOperationException("Import exactly one " + id + " of type " + type.Name + ".");
        return matches[0];
    }
    private static void CheckType<T>(string id) where T : CardEffect
    {
        var asset = AssetDatabase.LoadMainAssetAtPath(Folder + "/" + id + ".asset");
        if (asset != null && !(asset is T)) throw new InvalidOperationException("Effect type conflict: " + id);
    }
    private static T GetEffect<T>(string id) where T : CardEffect
    {
        string path = Folder + "/" + id + ".asset";
        var effect = AssetDatabase.LoadAssetAtPath<T>(path);
        if (effect != null) return effect;
        effect = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(effect, path);
        Undo.RegisterCreatedObjectUndo(effect, "Create Chloe/Sebastian effect");
        return effect;
    }
    private static void Edit(UnityEngine.Object asset, Action<SerializedObject> change)
    {
        Undo.RecordObject(asset, "Initialize Chloe/Sebastian");
        var serialized = new SerializedObject(asset);
        change(serialized);
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(asset);
    }
    private static void SetText(CardData card, List<List<string>> rows, string english, string korean)
    {
        Edit(card, s => s.FindProperty("description").stringValue = english);
        string key = card.CardID + "_TEXT";
        var row = rows.Skip(1).FirstOrDefault(r => r[0] == key);
        if (row == null) rows.Add(new List<string> { key, english, korean });
        else { row[1] = english; row[2] = korean; }
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }
}
