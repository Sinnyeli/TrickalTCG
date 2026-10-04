using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class DragonAbilityInitializer
{
    private const string Folder = "Assets/Cards/Effects/DragonBatch";
    public static void Build()
    {
        var cards = CardDataCsv.FindCards();
        var annette = Require(cards, "A_ANNETTE");
        var daya = Require(cards, "A_DAYA");
        var pure = Require(cards, "A_DAYAPURE");
        var jade = Require(cards, "A_JADE");
        var netty = Require(cards, "A_NETTY");
        var opal = Require(cards, "A_OPAL");
        var rows = CardDataCsv.ReadLocalizationRows();
        EnsureFolder(Folder);
        var forced = Make<AnnetteForcedAttackEffect>("FX_AnnetteForcedAttack");
        Edit(forced, s => Target(s, EffectTargetType.FriendlyUnit));
        Edit(annette, s => s.FindProperty("battlecry").objectReferenceValue = forced);
        var rule = Make<DayaPlayRuleEffect>("FX_DayaPaymentForm");
        Edit(rule, s => { s.FindProperty("baseForm").objectReferenceValue = daya; s.FindProperty("pureShine").objectReferenceValue = pure; });
        ConfigureDaya(daya, rule, 6, true);
        ConfigureDaya(pure, rule, 9, false);
        var mana = Make<IncreaseMaxManaEffect>("FX_JadeEmptyMaximumMana1");
        Edit(mana, s => { Target(s, EffectTargetType.None); s.FindProperty("amount").intValue = 1; });
        Edit(jade, s => s.FindProperty("battlecry").objectReferenceValue = mana);
        var equip = Make<NettyEquipFromHandEffect>("FX_NettyEquipRandomFromHand");
        Edit(equip, s => Target(s, EffectTargetType.None));
        Edit(netty, s => s.FindProperty("battlecry").objectReferenceValue = equip);
        var penalty = Make<OpalDamagePenaltyEffect>("FX_OpalExtraDamage1");
        Edit(penalty, s => Target(s, EffectTargetType.None));
        Edit(opal, s => s.FindProperty("passive").objectReferenceValue = penalty);
        Text(annette, rows, "Battlecry: Another friendly unit attacks a random enemy unit.", "등장: 다른 아군 유닛이 무작위 적 유닛을 공격합니다.");
        Text(daya, rows, "Costs 6. With 9 or more available mana, costs 9 and enters as Pure Shine.", "현재 마나가 9 이상이면 비용 9로 퓨어샤인이 되어 등장합니다.");
        Text(pure, rows, "", "");
        Text(jade, rows, "Battlecry: Gain 1 empty maximum mana.", "등장: 빈 최대 마나 +1.");
        Text(netty, rows, "Battlecry: Equip a random Artifact from your hand for free.", "등장: 패의 무작위 아티팩트를 무료로 장착합니다.");
        Text(opal, rows, "Takes 1 additional damage from each damage source.", "받는 피해 +1.");
        var csv = new StringBuilder(); foreach (var row in rows) CardDataCsv.AppendRow(csv, row);
        File.WriteAllText(CardDataCsv.LocalizationPath, csv.ToString(), new UTF8Encoding(true));
        AssetDatabase.SaveAssets();
        Debug.Log("Annette, Daya/Pure Shine, Jade, Netty and Opal configured. Daya form stats preserved.");
    }
    private static ApostleData Require(List<CardData> cards, string id)
    {
        var matches = cards.Where(c => c.CardID == id).ToList();
        if (matches.Count != 1 || !(matches[0] is ApostleData)) throw new InvalidOperationException("Import exactly one ApostleData " + id + ".");
        return (ApostleData)matches[0];
    }
    private static void ConfigureDaya(ApostleData card, DayaPlayRuleEffect rule, int cost, bool collectible)
    {
        Edit(card, s => {
            s.FindProperty("playRule").objectReferenceValue = rule;
            s.FindProperty("manaCost").intValue = cost;
            s.FindProperty("collectible").boolValue = collectible;
            foreach (string slot in new[] { "battlecry", "deathrattle", "passive", "resonance", "turnStart", "turnEnd", "onDamageTaken", "onAttack" })
                s.FindProperty(slot).objectReferenceValue = null;
            s.FindProperty("additionalPassives").arraySize = 0;
            s.FindProperty("keywords").arraySize = 0;
            s.FindProperty("onDrawEffects").arraySize = 0;
        });
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
