using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Reuses cards by CardID and builds explicitly configured effect assets.</summary>
public static class CardInitializationBuilder
{
    private const string EffectsFolder = "Assets/Cards/Effects/Initialized";
    private static Dictionary<string, CardData> cards;
    private static readonly List<string> report = new List<string>();

    [MenuItem("Tools/Cards/Initialize All Available Card Effects")]
    public static void InitializeAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        int succeeded = 0;
        var failures = new List<string>();
        RunBatch("Reference cards and basic effects", Build, failures, ref succeeded);
        RunBatch("Random Coin", InitializeRandomCoin, failures, ref succeeded);
        RunBatch("Farming", FarmEffectInitializer.Build, failures, ref succeeded);
        RunBatch("Chloe and Sebastian", ChloeSebastianInitializer.Build, failures, ref succeeded);
        RunBatch("Trigger effects", TriggerEffectInitializer.Build, failures, ref succeeded);
        RunBatch("Five reusable abilities", AbilityBatchInitializer.Build, failures, ref succeeded);
        RunBatch("Advanced reusable abilities", AdvancedAbilityBatchInitializer.Build, failures, ref succeeded);
        RunBatch("Komi Margo and Sparrot", BeastfolkAbilityInitializer.Build, failures, ref succeeded);
        RunBatch("Dragon abilities", DragonAbilityInitializer.Build, failures, ref succeeded);
        RunBatch("Dragon second batch", DragonSecondBatchInitializer.Build, failures, ref succeeded);
        RunBatch("Karen Erpin and Mayo", FairyAbilityInitializer.Build, failures, ref succeeded);
        RunBatch("Elemental abilities", ElementalAbilityInitializer.Build, failures, ref succeeded);
        RunBatch("Elf abilities and M.E.O.W.", ElfAbilityInitializer.Build, failures, ref succeeded);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        string summary = succeeded + "/13 initialization batches completed.";
        if (failures.Count > 0)
            Debug.LogError(summary + " Failed batches: " + string.Join("; ", failures) +
                ". Earlier successful changes remain saved; fix the missing requirements and rerun.");
        else Debug.Log(summary + " Gameplay verification is still required.");
    }

    private static void RunBatch(string name, Action action, List<string> failures, ref int succeeded)
    {
        try { action(); succeeded++; }
        catch (Exception error) { failures.Add(name + ": " + error.Message); Debug.LogException(error); }
    }

    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Run card initialization outside Play Mode.");
            return;
        }
        try
        {
            report.Clear();
            newTokens.Clear();
            cards = new Dictionary<string, CardData>(StringComparer.Ordinal);
            foreach (CardData card in CardDataCsv.FindCards())
            {
                if (string.IsNullOrWhiteSpace(card.CardID)) continue;
                if (cards.ContainsKey(card.CardID))
                    throw new InvalidOperationException("Duplicate CardID: " + card.CardID);
                cards.Add(card.CardID, card);
            }
            // Preflight references already supplied by the project/imported catalog.
            Require("S_BREAD"); Require("S_VOW"); Require("S_TUMBLR");
            Require("M_SEBASTIAN"); Require("M_PUMPKIN"); Require("S_COIN");
            CardDataCsv.ReadLocalizationRows();
            if (AssetDatabase.LoadAssetAtPath<CardDatabase>("Assets/Cards/Database/CardDatabase.asset") == null)
                throw new InvalidOperationException("CardDatabase.asset is missing.");
            Require("S_MINT");
            EnsureFolder(EffectsFolder);
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Initialize reference cards and effects");

            var carrot = Token<MonsterData>("M_CARROT", "Carrot", 1);
            ConfigureNewMinion(carrot, 1, 1, CardRace.Vegetable, CardKeyword.Bypass);
            var buffArtifact = Token<ArtifactData>("AR_TOKEN_BUFF11", "Small Charm", 1);
            if (IsNew(buffArtifact))
            {
                Edit(buffArtifact, s => { Int(s, "attackBonus", 1); Int(s, "healthBonus", 1); });
                MarkConfigured(buffArtifact);
            }
            Localize(carrot, "Carrot", "당근", "Bypass.", "우회.");
            Localize(buffArtifact, "Small Charm", "작은 부적", "", "");
            report.Add("Carrot and Small Charm use provisional cost 1. Existing token stats are preserved.");

            var buff11 = Buff("FX_BuffFriendly11", 1, 1);
            var buff22 = Buff("FX_BuffFriendly22", 2, 2);
            var monster22 = Buff("FX_BuffMonster22", 2, 2, EffectTargetFilter.CardType, CardType.Monster);
            var apostle12 = Buff("FX_BuffApostle12", 1, 2, EffectTargetFilter.CardType, CardType.Apostle);
            var breadBuff = Buff("FX_BuffAny11", 1, 1);
            Edit(breadBuff, x => Enum(x, "targetType", (int)EffectTargetType.AnyUnit));
            Spell("S_BREAD", breadBuff);

            Spell("S_SELF_IMPROVEMENT", buff11);
            Spell("S_HEALTHY_BODY", buff22);
            Spell("S_PROMOTION", monster22);
            Spell("S_PERSONAL_TRAINING", apostle12);
            Spell("S_APPRENTICE_MAGE", NumberEffect<DamageEffect>("FX_DamageEnemyTarget3", EffectTargetType.EnemyTarget, 3));
            Spell("S_SCHOLAR", NumberEffect<DrawCardEffect>("FX_Draw2", EffectTargetType.None, 2));
            Spell("S_EFFICIENT_RECOVERY", NumberEffect<HealEffect>("FX_HealAllFriendly2", EffectTargetType.AllFriendlyUnits, 2));
            Spell("S_WARM_HEART", NumberEffect<HealEffect>("FX_HealAnyTarget5", EffectTargetType.AnyTarget, 5));
            Spell("S_AROMATHERAPY", Effect<FullHealEffect>("FX_FullHealAll", EffectTargetType.AllUnits));
            var silence = Effect<SilenceEffect>("FX_SilenceUnit", EffectTargetType.AnyUnit);
            Spell("S_VOW", silence);
            Spell("S_TUMBLR", Effect<BounceEffect>("FX_BounceUnit", EffectTargetType.AnyUnit));
            var heal3 = NumberEffect<HealEffect>("FX_HealUnit3", EffectTargetType.FriendlyUnit, 3);
            var reason = Effect<SharedTargetSequenceEffect>("FX_SilenceThenHeal3", EffectTargetType.FriendlyUnit);
            Edit(reason, s => Objects(s, "effects", new UnityEngine.Object[] { silence, heal3 }));
            Spell("S_THREAD_OF_REASON", reason);
            PositionSpell("S_VANGUARD", PositionalBuffEffect.Position.Leftmost, 3, 1);
            PositionSpell("S_CENTER_GOOD", PositionalBuffEffect.Position.Center, 2, 2);
            PositionSpell("S_REAR_GUARD", PositionalBuffEffect.Position.Rightmost, 1, 3);

            var bread = Create("FX_CreateBread", "S_BREAD");
            var tumbler = Create("FX_CreateTumbler", "S_TUMBLR");
            var quiet = Create("FX_CreateQuiet", "S_VOW");
            var charm = Create("FX_CreateSmallCharm", "AR_TOKEN_BUFF11");
            var coin = Create("FX_CreateRandomCoin", "S_COIN");
            var pumpkin = Summon("FX_SummonPumpkin", "M_PUMPKIN", 1);
            var sebastian = Summon("FX_SummonSebastian", "M_SEBASTIAN", 1);
            var vegetables = cards.Values.Where(c => c is MonsterData m && m.cardRace == CardRace.Vegetable).ToList();
            var machines = cards.Values.Where(c => c is MonsterData m && m.cardRace == CardRace.Machine && m.health > 0).ToList();
            var artifacts = cards.Values.Where(c => c is ArtifactData && c.Collectible).ToList();
            var cheapArtifacts = artifacts.Where(c => c.manaCost <= 2).ToList();
            var randomVegetable = RandomCreate("FX_CreateRandomVegetable", vegetables);
            var randomMachine = RandomCreate("FX_CreateRandomMachine", machines);
            var cheapArtifact = RandomCreate("FX_CreateCheapArtifact", cheapArtifacts);
            Wire("A_CHLOE", "battlecry", sebastian);

            Wire("A_SASHA", "battlecry", tumbler);
            Wire("A_SKIA", "battlecry", quiet);
            Wire("A_THYST", "battlecry", cheapArtifact);
            Wire("A_ORE", "battlecry", randomMachine);
            Wire("A_PATRA", "battlecry", Create("FX_CreateMintBread", "S_MINT"));
            Wire("A_SHUPANG", "battlecry", NumberEffect<DrawCardEffect>("FX_Draw1", EffectTargetType.None, 1));
            Wire("A_SHUPANG", "turnEnd", Effect<BounceEffect>("FX_BounceSelf", EffectTargetType.Self));
            Wire("M_BULHYOJASON", "deathrattle", charm);
            Wire("M_LOW_SUGAR_FAIRY", "battlecry", randomVegetable);
            Wire("M_HIGH_SUGAR_FAIRY", "battlecry", bread);
            Wire("M_HOBAGING", "deathrattle", pumpkin);
            Wire("M_MOKMAEKKIM", "deathrattle", bread);
            Wire("M_CRUMB", "deathrattle", bread);
            Wire("M_NEW_HEART_SAFE", "deathrattle", coin);
            Wire("M_MOKDORYONG", "battlecry", NumberEffect<DamageEffect>("FX_DamageAnyTarget1", EffectTargetType.AnyTarget, 1));
            report.Add("Pending latest-design abilities: Mayo collectible pool; Polang Fairy aura; Joanne Fairy-enter trigger; Ricota enemy-death cooking; Ashur Bread-use Fireball; Shupang attack draw.");
            report.Add("Cheap artifacts currently means collectible cost <= 2; center targeting chooses the left center on an even board. Review these provisional conventions.");
            RegisterTokens();
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Design");
            File.WriteAllText("Design/CardInitializationReport.md", "# Initialization report\n\n" +
                string.Join("\n", report.Select(line => "- " + line)), new UTF8Encoding(false));
            AssetDatabase.Refresh();
            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("Reference cards and basic effects initialized. See Design/CardInitializationReport.md.");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            throw;
        }
    }

    // A newly created token is tracked only for the current run; existing cards are not restatted.
    private static readonly HashSet<CardData> newTokens = new HashSet<CardData>();
    private static bool IsNew(CardData card) => newTokens.Contains(card);
    private static void MarkConfigured(CardData card) => newTokens.Remove(card);
    private static CardData Require(string id)
    {
        if (!cards.TryGetValue(id, out CardData card))
            throw new InvalidOperationException("Missing card " + id + ". Import the card CSV first.");
        return card;
    }

    private static T Token<T>(string id, string name, int cost) where T : CardData
    {
        if (cards.TryGetValue(id, out CardData existing))
        {
            if (!(existing is T typed)) throw new InvalidOperationException(id + " has the wrong card type.");
            Edit(typed, s => s.FindProperty("collectible").boolValue = false);
            return typed;
        }
        string folder = "Assets/Cards/" + typeof(T).Name.Replace("Data", "");
        EnsureFolder(folder);
        T card = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(card, AssetDatabase.GenerateUniqueAssetPath(folder + "/" + id + ".asset"));
        Undo.RegisterCreatedObjectUndo(card, "Create token card");
        Edit(card, s =>
        {
            s.FindProperty("cardID").stringValue = id;
            s.FindProperty("cardName").stringValue = name;
            Int(s, "manaCost", cost);
            s.FindProperty("collectible").boolValue = false;
        });
        cards.Add(id, card);
        newTokens.Add(card);
        report.Add("Created non-collectible token " + id + ".");
        return card;
    }

    private static void ConfigureNewMinion(MinionData card, int attack, int health, CardRace race, CardKeyword keyword)
    {
        if (!IsNew(card)) return;
        Edit(card, s =>
        {
            Int(s, "attack", attack); Int(s, "health", health); Enum(s, "cardRace", (int)race);
            SerializedProperty list = s.FindProperty("keywords"); list.arraySize = 1;
            list.GetArrayElementAtIndex(0).enumValueIndex = (int)keyword;
        });
        MarkConfigured(card);
    }

    private static T Effect<T>(string id, EffectTargetType target) where T : CardEffect
    {
        string path = EffectsFolder + "/" + id + ".asset";
        T effect = AssetDatabase.LoadAssetAtPath<T>(path);
        if (effect == null)
        {
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
                throw new InvalidOperationException("Effect type conflict at " + path);
            effect = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(effect, path);
            Undo.RegisterCreatedObjectUndo(effect, "Create card effect");
        }
        Edit(effect, s => { Enum(s, "targetType", (int)target); Enum(s, "targetFilter", (int)EffectTargetFilter.None); });
        return effect;
    }

    private static BuffEffect Buff(string id, int attack, int health,
        EffectTargetFilter filter = EffectTargetFilter.None, CardType type = CardType.Monster)
    {
        var effect = Effect<BuffEffect>(id, EffectTargetType.FriendlyUnit);
        Edit(effect, s => { Int(s, "attackAmount", attack); Int(s, "healthAmount", health);
            Enum(s, "targetFilter", (int)filter); Enum(s, "cardType", (int)type); });
        return effect;
    }

    private static T NumberEffect<T>(string id, EffectTargetType target, int amount) where T : CardEffect
    {
        T effect = Effect<T>(id, target);
        Edit(effect, s => { Int(s, "amount", amount);
            if (effect is DrawCardEffect) Enum(s, "drawType", (int)DrawType.Top); });
        return effect;
    }

    private static CreateCardsEffect Create(string id, string cardID, int amount = 1)
    {
        var effect = Effect<CreateCardsEffect>(id, EffectTargetType.None);
        Edit(effect, s => { s.FindProperty("cardToCreate").objectReferenceValue = Require(cardID); Int(s, "amount", amount); });
        return effect;
    }

    private static SummonEffect Summon(string id, string cardID, int amount)
    {
        var effect = Effect<SummonEffect>(id, EffectTargetType.None);
        Edit(effect, s => { Objects(s, "summonPool", new[] { Require(cardID) }); Int(s, "amount", amount);
            Enum(s, "selectionType", (int)SummonSelectionType.Selected); Enum(s, "summonSideMode", (int)SummonSideMode.SourceOwner); });
        return effect;
    }

    private static CreateRandomCardEffect RandomCreate(string id, List<CardData> pool)
    {
        if (pool.Count == 0) { report.Add("Pending " + id + ": no eligible cards in its pool."); return null; }
        var effect = Effect<CreateRandomCardEffect>(id, EffectTargetType.None);
        Edit(effect, s => { Objects(s, "cardPool", pool.ToArray()); Int(s, "amount", 1); });
        return effect;
    }

    private static void PositionSpell(string id, PositionalBuffEffect.Position position, int attack, int health)
    {
        var effect = Effect<PositionalBuffEffect>("FX_Position_" + position, EffectTargetType.None);
        Edit(effect, s => { Enum(s, "position", (int)position); Int(s, "attackAmount", attack); Int(s, "healthAmount", health); });
        Spell(id, effect);
    }

    public static void InitializeRandomCoin()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var matches = CardDataCsv.FindCards().Where(c => c.CardID == "S_COIN").ToList();
        if (matches.Count != 1 || !(matches[0] is SpellData))
            throw new InvalidOperationException("Import exactly one S_COIN SpellData before initialization.");
        EnsureFolder(EffectsFolder);
        var effect = Effect<RestoreManaEffect>("FX_RestoreRandomMana0To2", EffectTargetType.None);
        Edit(effect, s => {
            s.FindProperty("randomAmount").boolValue = true;
            Int(s, "minimumAmount", 0); Int(s, "maximumAmount", 2);
        });
        CardData coin = matches[0];
        Edit(coin, s => {
            Objects(s, "spellEffects", new UnityEngine.Object[] { effect });
            s.FindProperty("description").stringValue = "Restore 0–2 available mana at random.";
        });
        var rows = CardDataCsv.ReadLocalizationRows();
        string key = coin.CardID + "_TEXT";
        var row = rows.Skip(1).FirstOrDefault(r => r[0] == key);
        if (row == null) { row = new List<string> { key, "", "" }; rows.Add(row); }
        row[1] = "Restore 0–2 available mana at random.";
        row[2] = "마나를 무작위로 0~2 회복합니다.";
        var csv = new StringBuilder();
        foreach (var entry in rows) CardDataCsv.AppendRow(csv, entry);
        File.WriteAllText(CardDataCsv.LocalizationPath, csv.ToString(), new UTF8Encoding(true));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("S_COIN initialized: restore 0–2 available mana; maximum mana is unchanged.");
    }

    private static void Spell(string id, CardEffect effect) => Wire(id, "spellEffects", effect, true);
    private static void Wire(string id, string slot, CardEffect effect, bool list = false)
    {
        if (!cards.TryGetValue(id, out CardData card)) { report.Add("Skipped " + id + ": import its card row first."); return; }
        if (effect == null) return;
        Edit(card, s =>
        {
            SerializedProperty property = s.FindProperty(slot);
            if (property == null) throw new InvalidOperationException(id + " does not have " + slot);
            if (list) { property.arraySize = 1; property.GetArrayElementAtIndex(0).objectReferenceValue = effect; }
            else property.objectReferenceValue = effect;
        });
        report.Add(id + " / " + slot + " → " + effect.name);
    }

    private static void RegisterTokens()
    {
        CardDatabase database = AssetDatabase.LoadAssetAtPath<CardDatabase>("Assets/Cards/Database/CardDatabase.asset");
        if (database == null) throw new InvalidOperationException("CardDatabase.asset is missing.");
        Edit(database, s =>
        {
            SerializedProperty list = s.FindProperty("allCards");
            foreach (string id in new[] { "M_CARROT", "AR_TOKEN_BUFF11" })
            {
                CardData card = Require(id);
                bool present = false;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue == card) { present = true; break; }
                if (present) continue;
                int index = list.arraySize++; list.GetArrayElementAtIndex(index).objectReferenceValue = card;
            }
        });
    }

    private static void Localize(CardData card, string englishName, string koreanName, string englishText, string koreanText)
    {
        var rows = CardDataCsv.ReadLocalizationRows();
        foreach (var entry in new[] { new[] { card.CardID + "_NAME", englishName, koreanName }, new[] { card.CardID + "_TEXT", englishText, koreanText } })
        {
            var existing = rows.Skip(1).FirstOrDefault(row => row[0] == entry[0]);
            if (existing == null) rows.Add(entry.ToList());
            else { if (string.IsNullOrWhiteSpace(existing[1])) existing[1] = entry[1];
                if (string.IsNullOrWhiteSpace(existing[2])) existing[2] = entry[2]; }
        }
        var csv = new StringBuilder(); foreach (var row in rows) CardDataCsv.AppendRow(csv, row);
        File.WriteAllText(CardDataCsv.LocalizationPath, csv.ToString(), new UTF8Encoding(true));
    }

    private static void Edit(UnityEngine.Object asset, Action<SerializedObject> configure)
    {
        Undo.RecordObject(asset, "Configure card initialization");
        var serialized = new SerializedObject(asset); configure(serialized);
        serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(asset);
    }
    private static void Int(SerializedObject s, string name, int value) => s.FindProperty(name).intValue = value;
    private static void Enum(SerializedObject s, string name, int value) => s.FindProperty(name).enumValueIndex = value;
    private static void Objects(SerializedObject s, string name, UnityEngine.Object[] values)
    {
        SerializedProperty list = s.FindProperty(name); list.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
