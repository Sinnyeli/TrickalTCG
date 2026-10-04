using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Invoked by the existing unified initializer; no additional menu.
public static class BeastfolkAbilityInitializer
{
    private const string Folder = "Assets/Cards/Effects/Beastfolk";
    public static void Build()
    {
        var cards = CardDataCsv.FindCards();
        var komi = Require(cards, "A_KOMI");
        var margo = Require(cards, "A_MARGO");
        var sparrot = Require(cards, "A_SPARROT");
        var rows = CardDataCsv.ReadLocalizationRows();
        EnsureFolder(Folder);
        var komiEffect = Make<KomiTurnEndEffect>("FX_KomiNoAttackTurnEnd");
        Edit(komiEffect, s => Target(s, EffectTargetType.None));
        Edit(komi, s => s.FindProperty("turnEnd").objectReferenceValue = komiEffect);
        var margoEffect = Make<MargoAuraEffect>("FX_MargoOtherBeastfolkAttack");
        Edit(margoEffect, s => Target(s, EffectTargetType.None));
        Edit(margo, s => s.FindProperty("passive").objectReferenceValue = margoEffect);
        Edit(sparrot, s => {
            var list = s.FindProperty("keywords");
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).enumValueIndex == (int)CardKeyword.Rush) return;
            int index = list.arraySize++; list.GetArrayElementAtIndex(index).enumValueIndex = (int)CardKeyword.Rush;
        });
        Text(komi, rows, "At the end of your turn, if this did not attack, gain +1/+1.", "내 턴 종료: 공격하지 않았다면 +1/+1.");
        Text(margo, rows, "Other friendly Beastfolk have +1 Attack.", "다른 아군 수인 공격력 +1.");
        Text(sparrot, rows, "Rush. While in your deck, summon this when you play another Apostle.", "속공. 덱에 있을 때 다른 사도를 내면 소환됩니다.");
        var csv = new StringBuilder(); foreach (var row in rows) CardDataCsv.AppendRow(csv, row);
        File.WriteAllText(CardDataCsv.LocalizationPath, csv.ToString(), new UTF8Encoding(true));
        ConfigureScene();
        AssetDatabase.SaveAssets();
        Debug.Log("Komi and Margo bound; Sparrot Rush configured. Save the gameplay scene after component installation.");
    }
    private static CardData Require(List<CardData> cards, string id)
    {
        var matches = cards.Where(c => c.CardID == id).ToList();
        if (matches.Count != 1 || !(matches[0] is ApostleData))
            throw new InvalidOperationException("Import exactly one ApostleData " + id + " first.");
        return matches[0];
    }
    private static void ConfigureScene()
    {
        var listeners = UnityEngine.Object.FindObjectsByType<SparrotDeckAbility>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (listeners.Length > 1) throw new InvalidOperationException("Multiple SparrotDeckAbility components found. Keep one per gameplay scene.");
        if (listeners.Length == 1)
        {
            if (!listeners[0].enabled) { Undo.RecordObject(listeners[0], "Enable Sparrot ability"); listeners[0].enabled = true; }
            if (!listeners[0].gameObject.activeInHierarchy) Debug.LogWarning("Sparrot listener is on an inactive object; activate its scene object.");
            EditorSceneManager.MarkSceneDirty(listeners[0].gameObject.scene);
            return;
        }
        var managers = UnityEngine.Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (managers.Length != 1)
        {
            Debug.LogWarning("Card bindings saved, but Sparrot scene setup skipped. Open the gameplay scene containing one GameManager and rerun initialization.");
            return;
        }
        Undo.AddComponent<SparrotDeckAbility>(managers[0].gameObject);
        EditorSceneManager.MarkSceneDirty(managers[0].gameObject.scene);
    }
    private static void Target(SerializedObject s, EffectTargetType target)
    {
        s.FindProperty("targetType").enumValueIndex = (int)target;
        s.FindProperty("targetFilter").enumValueIndex = (int)EffectTargetFilter.None;
    }
    private static void Text(CardData card, List<List<string>> rows, string english, string korean)
    {
        Edit(card, s => s.FindProperty("description").stringValue = english);
        string key = card.CardID + "_TEXT"; var row = rows.Skip(1).FirstOrDefault(r => r[0] == key);
        if (row == null) rows.Add(new List<string> { key, english, korean }); else { row[1] = english; row[2] = korean; }
    }
    private static T Make<T>(string name) where T : ScriptableObject
    {
        string path = Folder + "/" + name + ".asset";
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("Effect type conflict: " + path);
        asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path);
        Undo.RegisterCreatedObjectUndo(asset, "Create ability batch effect"); return asset;
    }
    private static void Edit(UnityEngine.Object asset, Action<SerializedObject> action)
    {
        Undo.RecordObject(asset, "Configure ability batch"); var s = new SerializedObject(asset); action(s); s.ApplyModifiedProperties(); EditorUtility.SetDirty(asset);
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
