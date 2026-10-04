using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Called only by Initialize All Available Card Effects; no separate menu.
public static class AbilityBatchInitializer
{
    private const string Folder = "Assets/Cards/Effects/AbilityBatch";
    public static void Build()
    {
        var cards = CardDataCsv.FindCards();
        var duplicates = cards.Where(c => !string.IsNullOrWhiteSpace(c.CardID)).GroupBy(c => c.CardID).Where(g => g.Count() > 1);
        if (duplicates.Any()) throw new InvalidOperationException("Duplicate CardIDs: " + string.Join(", ", duplicates.Select(g => g.Key)));
        EnsureFolder(Folder);
        var freeze = Make<FreezeEffect>("FX_FreezeDamagedUnit");
        Edit(freeze, s => Target(s, EffectTargetType.TriggerCard));
        var grant = Make<GrantKeywordEffect>("FX_GrantTauntExample");
        Edit(grant, s => { Target(s, EffectTargetType.FriendlyUnit); s.FindProperty("keyword").enumValueIndex = (int)CardKeyword.Taunt; s.FindProperty("turnEnds").intValue = 0; });
        var timed = Make<TimedModifierEffect>("FX_TemporaryBuff11Example");
        Edit(timed, s => { Target(s, EffectTargetType.FriendlyUnit); s.FindProperty("attackAmount").intValue = 1; s.FindProperty("healthAmount").intValue = 1; s.FindProperty("turnEnds").intValue = 1; });
        var refresh = Make<RefreshAttackEffect>("FX_RefreshAttackExample");
        Edit(refresh, s => Target(s, EffectTargetType.Self));
        var summon = Make<SummonFromDeckEffect>("FX_SummonThreeFromDeck");
        Edit(summon, s => { Target(s, EffectTargetType.None); s.FindProperty("amount").intValue = 3; });
        var trigger = Make<TriggeredEffect>("FX_FridgeDamageFreeze");
        Edit(trigger, s => {
            Target(s, EffectTargetType.None);
            s.FindProperty("triggerType").enumValueIndex = (int)CardTriggerType.DamageDealt;
            s.FindProperty("actorMustBeSelf").boolValue = true;
            s.FindProperty("matchRole").enumValueIndex = (int)TriggeredEffect.EventRole.Subject;
            s.FindProperty("requiredOccurrences").intValue = 1;
            s.FindProperty("repeatAfterThreshold").boolValue = true;
            s.FindProperty("conditions").arraySize = 0;
            var list = s.FindProperty("effects"); list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = freeze;
        });
        var rows = CardDataCsv.ReadLocalizationRows();
        var fridge = cards.SingleOrDefault(c => c.CardID == "M_FRIDGE");
        if (fridge is MonsterData)
        {
            Edit(fridge, s => {
                if (s.FindProperty("passive").objectReferenceValue == trigger) return;
                var list = s.FindProperty("additionalPassives");
                for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == trigger) return;
                int index = list.arraySize++; list.GetArrayElementAtIndex(index).objectReferenceValue = trigger;
            });
            Text(fridge, rows, "Freeze units this damages.", "피해를 준 유닛을 얼립니다.");
            if (((MonsterData)fridge).health <= 0) Debug.LogWarning("M_FRIDGE ability assigned, but positive health/stats must be supplied before testing.");
        }
        else Debug.LogWarning("M_FRIDGE not imported as MonsterData; freeze assignment skipped.");
        var promotion = cards.SingleOrDefault(c => c.CardID == "S_GROUP_PROMOTION");
        if (promotion is SpellData)
        {
            Edit(promotion, s => { var list = s.FindProperty("spellEffects"); list.arraySize = 1; list.GetArrayElementAtIndex(0).objectReferenceValue = summon; });
            Text(promotion, rows, "Summon 3 random units from your deck.", "내 덱에서 무작위 유닛 3개를 소환합니다.");
        }
        else Debug.LogWarning("S_GROUP_PROMOTION not imported as SpellData; deck-summon assignment skipped.");
        var csv = new StringBuilder(); foreach (var row in rows) CardDataCsv.AppendRow(csv, row);
        File.WriteAllText(CardDataCsv.LocalizationPath, csv.ToString(), new UTF8Encoding(true));
        AssetDatabase.SaveAssets();
        Debug.Log("Five ability types configured. Keyword, timed buff and attack refresh assets are examples awaiting card-specific assignments. Card costs/stats preserved.");
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
