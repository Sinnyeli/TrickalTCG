using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class FairyAbilityInitializer
{
    private const string Folder = "Assets/Cards/Effects/FairyBatch";
    public static void Build()
    {
        var cards = CardDataCsv.FindCards();
        var karen = Require(cards, "A_KAREN");
        var erpin = Require(cards, "A_ERPIN");
        var mayo = Require(cards, "A_MAYO");
        var carrot = cards.SingleOrDefault(c => c.CardID == "M_CARROT");
        if (!(carrot is MonsterData)) throw new InvalidOperationException("Initialize M_CARROT first.");
        if (!(cards.SingleOrDefault(c => c.CardID == "S_BREAD") is SpellData)) throw new InvalidOperationException("Import S_BREAD first.");
        var rows = CardDataCsv.ReadLocalizationRows();
        EnsureFolder(Folder);
        var summon = Make<SummonEffect>("FX_KarenSummonCarrot");
        Edit(summon, s => {
            Target(s, EffectTargetType.None);
            s.FindProperty("amount").intValue = 1;
            s.FindProperty("selectionType").enumValueIndex = (int)SummonSelectionType.Selected;
            s.FindProperty("summonSideMode").enumValueIndex = (int)SummonSideMode.SourceOwner;
            var list = s.FindProperty("summonPool"); list.arraySize = 1; list.GetArrayElementAtIndex(0).objectReferenceValue = carrot;
        });
        Edit(karen, s => s.FindProperty("onDamageTaken").objectReferenceValue = summon);
        var bread = Make<ErpinCastBreadEffect>("FX_ErpinCastBread");
        Edit(bread, s => Target(s, EffectTargetType.None));
        Edit(erpin, s => s.FindProperty("battlecry").objectReferenceValue = bread);
        var merchandise = Make<MayoMerchandiseEffect>("FX_MayoCreateMerchandise");
        Edit(merchandise, s => Target(s, EffectTargetType.None));
        Edit(mayo, s => s.FindProperty("battlecry").objectReferenceValue = merchandise);
        var coco = Merchandise(cards, rows, "AR_MAYO_COCO", "Coco", "코코", MayoEquipmentEffect.Ability.IgnoreEndure,
            "Equipped unit ignores Endure when dealing damage.", "장착 유닛이 버티기를 무시합니다.");
        var gun = Merchandise(cards, rows, "AR_MAYO_ANESTHETIC_GUN", "Anesthetic Gun", "마취총", MayoEquipmentEffect.Ability.FreezeOnAttack,
            "On attack, freeze the attacked unit.", "공격 시 대상 유닛을 얼립니다.");
        var copy = Merchandise(cards, rows, "AR_MAYO_EXACTLY_ALIKE", "Exactly Alike", "완전또가틈", MayoEquipmentEffect.Ability.CopyRandomStatsOnEquip,
            "On equip, copy another random unit's current Attack and Health.", "장착 시 다른 무작위 유닛의 현재 공격력과 체력을 복사합니다.");
        Edit(merchandise, s => {
            var pool = s.FindProperty("merchandisePool");
            foreach (var card in new[] { coco, gun, copy })
            {
                bool present = false;
                for (int i = 0; i < pool.arraySize; i++) if (pool.GetArrayElementAtIndex(i).objectReferenceValue == card) present = true;
                if (!present) { int index = pool.arraySize++; pool.GetArrayElementAtIndex(index).objectReferenceValue = card; }
            }
        });
        var database = AssetDatabase.LoadAssetAtPath<CardDatabase>("Assets/Cards/Database/CardDatabase.asset");
        if (database == null) throw new InvalidOperationException("CardDatabase missing.");
        Edit(database, s => {
            var list = s.FindProperty("allCards");
            foreach (var card in new[] { coco, gun, copy })
            {
                bool present = false;
                for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == card) present = true;
                if (!present) { int index = list.arraySize++; list.GetArrayElementAtIndex(index).objectReferenceValue = card; }
            }
        });
        Text(karen, rows, "After taking damage and surviving, summon a Carrot.", "피해를 받고 생존하면 당근을 소환합니다.");
        Text(erpin, rows, "Battlecry: Cast all Bread in your hand on this unit for free.", "등장: 패의 모든 빵을 자신에게 무료로 사용합니다.");
        Text(mayo, rows, "Battlecry: Create a random non-collectible merchandise Artifact.", "등장: 무작위 수집품 1장을 생성합니다.");
        var csv = new StringBuilder(); foreach (var row in rows) CardDataCsv.AppendRow(csv, row);
        File.WriteAllText(CardDataCsv.LocalizationPath, csv.ToString(), new UTF8Encoding(true));
        AssetDatabase.SaveAssets();
        Debug.Log("Karen/Erpin/Mayo configured, including all three merchandise references.");
    }
    private static ApostleData Require(List<CardData> cards, string id)
    {
        var matches = cards.Where(c => c.CardID == id).ToList();
        if (matches.Count != 1 || !(matches[0] is ApostleData)) throw new InvalidOperationException("Import exactly one ApostleData " + id + ".");
        return (ApostleData)matches[0];
    }
    private static ArtifactData Merchandise(List<CardData> cards, List<List<string>> rows,
        string id, string englishName, string koreanName, MayoEquipmentEffect.Ability ability, string english, string korean)
    {
        var matches = cards.Where(c => c.CardID == id).ToList();
        if (matches.Count > 1 || (matches.Count == 1 && !(matches[0] is ArtifactData)))
            throw new InvalidOperationException("Invalid merchandise ID " + id);
        ArtifactData card;
        if (matches.Count == 1) card = (ArtifactData)matches[0];
        else
        {
            string folder = "Assets/Cards/Artifact/Mayo"; EnsureFolder(folder);
            card = ScriptableObject.CreateInstance<ArtifactData>();
            string path = folder + "/" + id + ".asset";
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("Asset path conflict: " + path);
            AssetDatabase.CreateAsset(card, path);
            Undo.RegisterCreatedObjectUndo(card, "Create Mayo merchandise"); cards.Add(card);
        }
        var effect = Make<MayoEquipmentEffect>("FX_" + id);
        Edit(effect, s => { Target(s, EffectTargetType.None); s.FindProperty("ability").enumValueIndex = (int)ability; });
        Edit(card, s => {
            s.FindProperty("cardID").stringValue = id; s.FindProperty("cardName").stringValue = englishName;
            s.FindProperty("manaCost").intValue = 1; s.FindProperty("collectible").boolValue = false;
            s.FindProperty("attackBonus").intValue = 0; s.FindProperty("healthBonus").intValue = 0;
            s.FindProperty("grantedKeywords").arraySize = 0;
            s.FindProperty("artifactEffect").objectReferenceValue = effect;
            s.FindProperty("deathrattle").objectReferenceValue = null;
            s.FindProperty("onDrawEffects").arraySize = 0;
        });
        Text(card, rows, english, korean);
        string key = id + "_NAME"; var nameRow = rows.Skip(1).FirstOrDefault(r => r[0] == key);
        if (nameRow == null) rows.Add(new List<string> { key, englishName, koreanName });
        else { nameRow[1] = englishName; nameRow[2] = koreanName; }
        return card;
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
