using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class ElfAbilityInitializer
{
    private const string Folder = "Assets/Cards/Effects/ElfBatch";
    public static void Build()
    {
        var cards = CardDataCsv.FindCards();
        var ids = new[] { "A_ALETTE", "A_RMELIA", "A_AMELIA", "A_CATHY", "A_ED", "A_EISHA", "A_ELENA", "A_HEIDI", "A_HILDE", "A_KANNA", "A_LAZY", "A_MAESTRO", "A_ORE", "A_RENEWA", "A_RISTI", "A_TAIDA" };
        foreach (var id in ids) Require(cards, id);
        var fridge = cards.SingleOrDefault(c => c.CardID == "M_FRIDGE") as MonsterData;
        if (fridge == null) throw new InvalidOperationException("M_FRIDGE MonsterData is required.");
        var database = AssetDatabase.LoadAssetAtPath<CardDatabase>("Assets/Cards/Database/CardDatabase.asset");
        if (database == null) throw new InvalidOperationException("CardDatabase.asset is required.");
        var rows = CardDataCsv.ReadLocalizationRows();
        EnsureFolder(Folder);
        var meow = Token<MonsterData>(cards, "M_MEOW", "M.E.O.W.", 0);
        Edit(meow, x => { x.FindProperty("attack").intValue = 1; x.FindProperty("health").intValue = 1; x.FindProperty("cardRace").enumValueIndex = (int)CardRace.Machine; });
        var farewell = Token<SpellData>(cards, "S_FAREWELL_NATA", "Farewell, Nata", 2);
        var damage = Make<DamageEffect>("FX_FarewellNataDamage10");
        Edit(damage, x => { Target(x, EffectTargetType.AllEnemyUnits); x.FindProperty("amount").intValue = 10; });
        Edit(farewell, x => Refs(x, "spellEffects", new UnityEngine.Object[] { damage }));
        Register(database, meow, farewell);
        var fusion = Bind<ElenaFusionEffect>(Require(cards, "A_ELENA"), "FX_ElenaFusion", EffectTargetType.None);
        Edit(fusion, x => x.FindProperty("meow").objectReferenceValue = meow);
        var ed = Bind<EdGraduationEffect>(Require(cards, "A_ED"), "FX_EdGraduation", EffectTargetType.None, "turnEnd");
        Edit(ed, x => x.FindProperty("farewell").objectReferenceValue = farewell);
        var r41 = Bind<ElenaAllyBuffEffect>(Require(cards, "A_RMELIA"), "FX_R41ElenaBuff", EffectTargetType.None);
        Edit(r41, x => x.FindProperty("buffElena").boolValue = false);
        var amelia = Bind<ElenaAllyBuffEffect>(Require(cards, "A_AMELIA"), "FX_AmeliaElenaBuff", EffectTargetType.None);
        Edit(amelia, x => x.FindProperty("buffElena").boolValue = true);
        var summon = Bind<SummonEffect>(Require(cards, "A_EISHA"), "FX_EishaFridge", EffectTargetType.None);
        Edit(summon, x => { Refs(x, "summonPool", new UnityEngine.Object[] { fridge }); x.FindProperty("amount").intValue = 1; x.FindProperty("selectionType").enumValueIndex = (int)SummonSelectionType.Selected; x.FindProperty("summonSideMode").enumValueIndex = (int)SummonSideMode.SourceOwner; });
        if (fridge.health <= 0) Debug.LogWarning("Eisha is assigned, but M_FRIDGE still needs positive health/stats before gameplay testing.");
        Bind<HeidiAttackBuffEffect>(Require(cards, "A_HEIDI"), "FX_HeidiAttackBuff", EffectTargetType.AnyUnit);
        var heal = Bind<HealEffect>(Require(cards, "A_HILDE"), "FX_HildeHeal4", EffectTargetType.FriendlyHero);
        Edit(heal, x => x.FindProperty("amount").intValue = 4);
        var kanna = Bind<DamageEffect>(Require(cards, "A_KANNA"), "FX_KannaDamage3", EffectTargetType.AnyUnit);
        Edit(kanna, x => x.FindProperty("amount").intValue = 3);
        var lazy = Bind<DamageEffect>(Require(cards, "A_LAZY"), "FX_LazyDamage1", EffectTargetType.AnyTarget);
        Edit(lazy, x => x.FindProperty("amount").intValue = 1);
        var ore = Bind<CreateRandomCardEffect>(Require(cards, "A_ORE"), "FX_OreMachineMonster", EffectTargetType.None);
        Edit(ore, x => { Refs(x, "cardPool", cards.OfType<MonsterData>().Where(m => m.HasRace(CardRace.Machine) && m.Collectible).Cast<UnityEngine.Object>().ToArray()); x.FindProperty("amount").intValue = 1; });
        Bind<RenewaProtectionEffect>(Require(cards, "A_RENEWA"), "FX_RenewaProtection", EffectTargetType.AllFriendlyUnits);
        var risti = Bind<CostModifierEffect>(Require(cards, "A_RISTI"), "FX_RistiDiscount", EffectTargetType.None);
        Edit(risti, x => { x.FindProperty("amount").intValue = -2; x.FindProperty("selection").enumValueIndex = (int)HandCardSelection.RandomMatching; });
        Bind<TaidaRestEffect>(Require(cards, "A_TAIDA"), "FX_TaidaRest", EffectTargetType.None, "onAttack");
        Edit(Require(cards, "A_MAESTRO"), x => { x.FindProperty("cardRace").enumValueIndex = (int)CardRace.Elf; var races = x.FindProperty("additionalRaces"); races.arraySize = 1; races.GetArrayElementAtIndex(0).enumValueIndex = (int)CardRace.Machine; });
        Keywords(Require(cards, "A_ALETTE"), CardKeyword.Taunt, CardKeyword.Endure);
        Keywords(Require(cards, "A_CATHY"), CardKeyword.Shock, CardKeyword.Stealth);
        Text(Require(cards, "A_ELENA"), rows, "Battlecry: Destroy your Machines, then summon M.E.O.W. with their combined current Attack, Health, keywords and abilities, including equipment. Elena is not consumed.", "등장: 아군 기계 유닛을 파괴한 뒤 현재 공격력, 체력, 키워드 및 장비를 포함한 능력을 합친 M.E.O.W.를 소환합니다. 엘레나는 파괴하지 않습니다.");
        Text(meow, rows, "Inherits the consumed Machines' current stats and abilities. Silence removes the inherited contributions, leaving a vanilla 1/1.", "흡수한 기계의 현재 능력치와 능력을 계승합니다. 침묵하면 계승 효과가 사라지고 능력 없는 1/1이 됩니다.");
        Text(Require(cards, "A_ED"), rows, "At the end of your turn, gain a Dream counter. At 3, create Farewell, Nata in your hand. Once per copy.", "내 턴 종료 시 꿈 카운터를 얻습니다. 3개가 되면 패에 '안녕, 나타'를 생성합니다. 각 개체당 한 번.");
        Text(farewell, rows, "Deal 10 damage to all enemy units.", "모든 적 유닛에게 피해를 10 줍니다.");
        Name(rows, meow.CardID, "M.E.O.W.", "M.E.O.W.");
        Name(rows, farewell.CardID, "Farewell, Nata", "안녕, 나타");
        var csv = new StringBuilder(); foreach (var row in rows) CardDataCsv.AppendRow(csv, row);
        File.WriteAllText(CardDataCsv.LocalizationPath, csv.ToString(), new UTF8Encoding(true));
        AssetDatabase.SaveAssets();
        Debug.Log("16 Elf units configured, including Elena/M.E.O.W., Ed and dual-race Maestro.");
    }
    private static T Token<T>(List<CardData> cards, string id, string name, int cost) where T : CardData
    {
        var matches = cards.Where(c => c.CardID == id).ToList();
        if (matches.Count > 1 || (matches.Count == 1 && !(matches[0] is T))) throw new InvalidOperationException("Token ID conflict: " + id);
        var card = matches.Count == 1 ? (T)matches[0] : Make<T>(id);
        Edit(card, x => { x.FindProperty("cardID").stringValue = id; x.FindProperty("cardName").stringValue = name; x.FindProperty("manaCost").intValue = cost; x.FindProperty("collectible").boolValue = false; });
        if (!cards.Contains(card)) cards.Add(card);
        return card;
    }
    private static void Register(CardDatabase database, params CardData[] cards)
    {
        Edit(database, x => { var list = x.FindProperty("allCards"); foreach (var card in cards) { bool found = false; for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == card) found = true; if (!found) { int i = list.arraySize++; list.GetArrayElementAtIndex(i).objectReferenceValue = card; } } });
    }
    private static void Name(List<List<string>> rows, string id, string english, string korean)
    {
        string key = id + "_NAME"; var row = rows.Skip(1).FirstOrDefault(r => r.Count > 0 && r[0] == key);
        if (row == null) rows.Add(new List<string> { key, english, korean }); else { while (row.Count < 3) row.Add(""); row[1] = english; row[2] = korean; }
    }
    private static void Keywords(CardData card, params CardKeyword[] keywords)
    {
        Edit(card, x => { var list = x.FindProperty("keywords"); list.arraySize = keywords.Length; for (int i = 0; i < keywords.Length; i++) list.GetArrayElementAtIndex(i).enumValueIndex = (int)keywords[i]; });
    }
    private static void Refs(SerializedObject x, string field, UnityEngine.Object[] values)
    {
        var list = x.FindProperty(field); list.arraySize = values.Length; for (int i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
    private static ApostleData Require(List<CardData> cards, string id)
    {
        var matches = cards.Where(c => c.CardID == id).ToList();
        if (matches.Count != 1 || !(matches[0] is ApostleData)) throw new InvalidOperationException("Import exactly one ApostleData " + id + ".");
        return (ApostleData)matches[0];
    }
    private static T Bind<T>(CardData card, string name, EffectTargetType target, string slot = "battlecry") where T : CardEffect
    {
        var effect = Make<T>(name); Edit(effect, x => Target(x, target));
        Edit(card, x => x.FindProperty(slot).objectReferenceValue = effect); return effect;
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
