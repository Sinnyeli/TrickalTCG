using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class DragonSecondBatchInitializer
{
    private const string Folder = "Assets/Cards/Effects/DragonSecondBatch";
    public static void Build()
    {
        var cards = CardDataCsv.FindCards();
        var pyra = Require(cards, "A_PYRA");
        var ritz = Require(cards, "A_RITZ");
        var rudd = Require(cards, "A_RUDD");
        var sylphyr = Require(cards, "A_SYLPHYR");
        var vivi = Require(cards, "A_VIVI");
        var rows = CardDataCsv.ReadLocalizationRows();
        EnsureFolder(Folder);
        Bind<PyraNextCardDiscountEffect>(pyra, "battlecry", "FX_PyraNextCardMinus2");
        Bind<RitzDefendSwapEffect>(ritz, "passive", "FX_RitzSwapWhenAttacked");
        Bind<RuddStatGainEffect>(rudd, "passive", "FX_RuddAdditional11");
        Bind<SylphyrFixedStatsEffect>(sylphyr, "passive", "FX_SylphyrFixed33");
        Bind<ViviHealthCombatEffect>(vivi, "passive", "FX_ViviHealthCombat");
        Edit(sylphyr, s => { s.FindProperty("attack").intValue = 3; s.FindProperty("health").intValue = 3; });
        Text(pyra, rows, "Battlecry: Your next card costs 2 less.", "등장: 다음 카드 비용 −2.");
        Text(ritz, rows, "When attacked, swap Attack and maximum Health before combat.", "공격받기 전 공격력과 최대 체력을 교환합니다.");
        Text(rudd, rows, "After gaining stats, gain an additional +1/+1. This bonus does not retrigger itself.", "능력치 획득 시 추가 +1/+1. 추가 강화는 재발동하지 않습니다.");
        Text(sylphyr, rows, "Attack and maximum Health are fixed at 3; ignore buffs and debuffs.", "공격력과 최대 체력은 3으로 고정됩니다.");
        Text(vivi, rows, "Friendly units deal combat damage equal to their current Health.", "아군 유닛은 현재 체력으로 전투 피해를 줍니다.");
        var csv = new StringBuilder(); foreach (var row in rows) CardDataCsv.AppendRow(csv, row);
        File.WriteAllText(CardDataCsv.LocalizationPath, csv.ToString(), new UTF8Encoding(true));
        AssetDatabase.SaveAssets();
        Debug.Log("Pyra, Ritz, Rudd, Sylphyr and Vivi configured.");
    }
    private static ApostleData Require(List<CardData> cards, string id)
    {
        var matches = cards.Where(c => c.CardID == id).ToList();
        if (matches.Count != 1 || !(matches[0] is ApostleData)) throw new InvalidOperationException("Import exactly one ApostleData " + id + ".");
        return (ApostleData)matches[0];
    }
    private static void Bind<T>(CardData card, string slot, string name) where T : CardEffect
    {
        var effect = Make<T>(name); Edit(effect, s => Target(s, EffectTargetType.None));
        Edit(card, s => s.FindProperty(slot).objectReferenceValue = effect);
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
