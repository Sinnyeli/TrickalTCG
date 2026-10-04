using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ElementalAbilityInitializer
{
    private const string Folder = "Assets/Cards/Effects/ElementalBatch";
    public static void Build()
    {
        var cards = CardDataCsv.FindCards();
        foreach (string id in new[] { "A_ARUKO", "A_BLANCHE", "A_INKLE", "A_MIRO", "A_MUTE" }) Require(cards, id);
        var rows = CardDataCsv.ReadLocalizationRows();
        EnsureFolder(Folder);
        Bind<ArukoDanceBattleEffect>(Require(cards, "A_ARUKO"), "FX_ArukoDance", EffectTargetType.None);
        Bind<BlancheAttackDamageEffect>(Require(cards, "A_BLANCHE"), "FX_BlancheAttackDamage", EffectTargetType.AnyUnit);
        Bind<InkleCopyHandSpellsEffect>(Require(cards, "A_INKLE"), "FX_InkleCopySpells", EffectTargetType.None);
        Bind<MiroFourDeckCopiesEffect>(Require(cards, "A_MIRO"), "FX_MiroFourCopies", EffectTargetType.AnyUnit);
        Bind<MuteDestroyTopDeckEffect>(Require(cards, "A_MUTE"), "FX_MuteDestroyTop", EffectTargetType.None);
        Text(Require(cards, "A_ARUKO"), rows, "Battlecry: Each unit attacks another random unit.", "등장: 모든 유닛이 다른 무작위 유닛을 공격합니다.");
        Text(Require(cards, "A_BLANCHE"), rows, "Battlecry: Deal damage to a unit equal to its Attack.", "등장: 유닛에게 그 공격력만큼 피해를 줍니다.");
        Text(Require(cards, "A_INKLE"), rows, "Battlecry: Create a copy of every spell in your hand.", "등장: 패의 모든 마법을 1장씩 복사합니다.");
        Text(Require(cards, "A_MIRO"), rows, "Battlecry: Shuffle 4 copies of a chosen unit into your deck.", "등장: 선택한 유닛의 복사본 4장을 내 덱에 섞습니다.");
        Text(Require(cards, "A_MUTE"), rows, "Battlecry: Destroy the top card of the opponent's deck.", "등장: 상대 덱 맨 위 카드 1장을 파괴합니다.");
        var csv = new StringBuilder(); foreach (var row in rows) CardDataCsv.AppendRow(csv, row);
        File.WriteAllText(CardDataCsv.LocalizationPath, csv.ToString(), new UTF8Encoding(true));
        AssetDatabase.SaveAssets();
        Debug.Log("Aruko, Blanche, Inkle, Miro and Mute configured.");
    }
    private static ApostleData Require(List<CardData> cards, string id)
    {
        var matches = cards.Where(c => c.CardID == id).ToList();
        if (matches.Count != 1 || !(matches[0] is ApostleData)) throw new InvalidOperationException("Import exactly one ApostleData " + id + ".");
        return (ApostleData)matches[0];
    }
    private static void Bind<T>(CardData card, string name, EffectTargetType target) where T : CardEffect
    {
        var effect = Make<T>(name); Edit(effect, s => Target(s, target));
        Edit(card, s => s.FindProperty("battlecry").objectReferenceValue = effect);
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
