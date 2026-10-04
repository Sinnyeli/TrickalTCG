using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AdvancedAbilityBatchInitializer
{
    private const string Folder = "Assets/Cards/Effects/AdvancedAbilityBatch";
    public static void Build()
    {
        var cards = CardDataCsv.FindCards();
        if (cards.Where(c => !string.IsNullOrWhiteSpace(c.CardID)).GroupBy(c => c.CardID).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Duplicate card IDs; fix these before initialization.");
        EnsureFolder(Folder);
        var copy = Make<CopyCardEffect>("FX_CopyFriendlyAs11");
        Edit(copy, s => {
            Target(s, EffectTargetType.FriendlyUnit);
            s.FindProperty("destination").enumValueIndex = (int)CopyCardEffect.Destination.Field;
            s.FindProperty("overrideStats").boolValue = true;
            s.FindProperty("attack").intValue = 1; s.FindProperty("health").intValue = 1;
        });
        BindSpell(cards, "S_AFTERIMAGE", copy);
        var health = Make<ModifyPlayerHealthEffect>("FX_IncreasePlayerHealth3");
        Edit(health, s => {
            Target(s, EffectTargetType.None);
            s.FindProperty("maximumHealthIncrease").intValue = 3;
            s.FindProperty("healAmount").intValue = 3;
        });
        BindSpell(cards, "S_HEALTH_BOOST", health);
        var swap = Make<StatOperationEffect>("FX_SwapAttackMaxHealth");
        Edit(swap, s => { Target(s, EffectTargetType.FriendlyUnit); s.FindProperty("operation").enumValueIndex = (int)StatOperationEffect.Operation.Swap; });
        var rush = Make<GrantKeywordEffect>("FX_RushUntilTurnEnd");
        Edit(rush, s => { Target(s, EffectTargetType.FriendlyUnit); s.FindProperty("keyword").enumValueIndex = (int)CardKeyword.Rush; s.FindProperty("turnEnds").intValue = 1; });
        var sequence = Make<SharedTargetSequenceEffect>("FX_TacticalSwapThenRush");
        Edit(sequence, s => {
            Target(s, EffectTargetType.FriendlyUnit);
            var list = s.FindProperty("effects"); list.arraySize = 2;
            list.GetArrayElementAtIndex(0).objectReferenceValue = swap;
            list.GetArrayElementAtIndex(1).objectReferenceValue = rush;
        });
        BindSpell(cards, "S_TACTICAL_MANUAL", sequence);
        var conditional = Make<ConditionalStatEffect>("FX_DamagedAttackBonusExample");
        Edit(conditional, s => { Target(s, EffectTargetType.Self); s.FindProperty("condition").enumValueIndex = (int)ConditionalStatEffect.Condition.Damaged; s.FindProperty("attackAmount").intValue = 2; });
        var random = Make<RandomOutcomeEffect>("FX_RandomOutcomeTemplate");
        Edit(random, s => Target(s, EffectTargetType.None));
        // Leave the configured outcome list intact on repeated runs. Do not invent roulette rules.
        AssetDatabase.SaveAssets();
        Debug.Log("Advanced batch configured: Afterimage, Health Boost, Tactical Manual where imported. Conditional attack and random outcome assets remain unassigned examples. Card costs preserved.");
    }
    private static void BindSpell(List<CardData> cards, string id, CardEffect effect)
    {
        var card = cards.SingleOrDefault(c => c.CardID == id);
        if (!(card is SpellData)) { Debug.LogWarning(id + " missing or wrong type; assignment skipped."); return; }
        Edit(card, s => { var list = s.FindProperty("spellEffects"); list.arraySize = 1; list.GetArrayElementAtIndex(0).objectReferenceValue = effect; });
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
