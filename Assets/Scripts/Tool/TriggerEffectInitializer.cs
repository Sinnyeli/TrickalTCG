using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TriggerEffectInitializer
{
    private const string Folder = "Assets/Cards/Effects/TriggerExtensions";
    [MenuItem("Tools/Cards/Initialize Shuro Ouros and Levi Reward")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var cards = CardDataCsv.FindCards();
        var duplicates = cards.Where(c => !string.IsNullOrWhiteSpace(c.CardID)).GroupBy(c => c.CardID).FirstOrDefault(g => g.Count() > 1);
        if (duplicates != null) throw new InvalidOperationException("Duplicate CardID: " + duplicates.Key);
        if (AssetDatabase.LoadAssetAtPath<CardDatabase>("Assets/Cards/Database/CardDatabase.asset") == null)
            throw new InvalidOperationException("CardDatabase.asset missing; import the catalog first.");
        CardDataCsv.ReadLocalizationRows();
        var shuro = cards.SingleOrDefault(c => c.CardID == "A_SHURO") as MinionData;
        var ouros = cards.SingleOrDefault(c => c.CardID == "A_OUROS") as MinionData;
        if (shuro == null || ouros == null)
        {
            Debug.LogError("Import A_SHURO and A_OUROS before initializing triggers.");
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/Cards/Effects"))
            AssetDatabase.CreateFolder("Assets/Cards", "Effects");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Cards/Effects", "TriggerExtensions");
        var gain = Make<GainDefeatedStatsEffect>("FX_ShuroGainDefeatedStats");
        Configure(gain, s => s.FindProperty("targetType").enumValueIndex = (int)EffectTargetType.TriggerCard);
        var transform = Make<TransformEffect>("FX_ShuroTransformOuros");
        Configure(transform, s => {
            s.FindProperty("targetType").enumValueIndex = (int)EffectTargetType.Self;
            s.FindProperty("transformInto").objectReferenceValue = ouros;
        });
        var growthTrigger = Trigger("FX_ShuroKillGrowth", 1, true, gain);
        var evolutionTrigger = Trigger("FX_ShuroThreeKills", 3, false, transform);
        Configure(shuro, s => {
            s.FindProperty("passive").objectReferenceValue = growthTrigger;
            var list = s.FindProperty("additionalPassives");
            // Keep unrelated additional passives; put evolution after growth.
            bool present = false;
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == evolutionTrigger) present = true;
            if (!present) { int i = list.arraySize; list.arraySize++; list.GetArrayElementAtIndex(i).objectReferenceValue = evolutionTrigger; }
        });
        var aura = Make<EnemyMonsterAuraEffect>("FX_OurosEnemyMonstersMinus11");
        Configure(ouros, s => s.FindProperty("passive").objectReferenceValue = aura);
        var reward = Make<PermanentSpellDiscountEffect>("FX_LeviPermanentSpellDiscount");
        Configure(reward, s => {
            s.FindProperty("amount").intValue = 1;
            s.FindProperty("rewardID").stringValue = "LEVI_UPGRADE";
            s.FindProperty("targetType").enumValueIndex = (int)EffectTargetType.None;
        });
        ConfigureLevi(cards, reward);
        var bread = cards.SingleOrDefault(c => c.CardID == "S_BREAD");
        if (bread != null)
        {
            var create = Make<CreateCardsEffect>("FX_TriggerCreateBread");
            Configure(create, s => {
                s.FindProperty("cardToCreate").objectReferenceValue = bread;
                s.FindProperty("amount").intValue = 1;
                s.FindProperty("targetType").enumValueIndex = (int)EffectTargetType.None;
            });
            var enemy = Make<EventCardTriggerCondition>("COND_EnemyEventCard");
            Configure(enemy, s => s.FindProperty("relation").enumValueIndex = (int)EventCardTriggerCondition.Relation.Enemy);
            var otherFriendly = Make<EventCardTriggerCondition>("COND_OtherFriendlyEventCard");
            Configure(otherFriendly, s => s.FindProperty("relation").enumValueIndex = (int)EventCardTriggerCondition.Relation.OtherFriendly);
            var fairy = Make<RaceTriggerCondition>("COND_FairyEventCard");
            Configure(fairy, s => s.FindProperty("requiredRace").enumValueIndex = (int)CardRace.Sprite);
            var ricota = cards.SingleOrDefault(c => c.CardID == "A_RICOTA");
            var joanne = cards.SingleOrDefault(c => c.CardID == "A_JOANNE");
            AttachBreadTrigger(ricota, "FX_RicotaEnemyDeathBread", CardTriggerType.UnitDied, create, enemy);
            AttachBreadTrigger(joanne, "FX_JoanneOtherFairyBread", CardTriggerType.UnitSummoned, create, otherFriendly, fairy);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Shuro and Ouros configured. Levi quest configured: five friendly spells in one turn while on field. Ensure TriggeredEffectManager is enabled in the gameplay scene.");
    }
    private static void ConfigureLevi(System.Collections.Generic.List<CardData> cards, CardEffect reward)
    {
        var levi = FindApostle(cards, "A_LEVI");
        if (levi == null)
        {
            levi = Make<ApostleData>("A_LEVI");
            Configure(levi, s => {
                s.FindProperty("cardID").stringValue = "A_LEVI";
                s.FindProperty("cardName").stringValue = "Levi";
                s.FindProperty("collectible").boolValue = true;
            });
            cards.Add(levi);
        }
        var upgraded = FindApostle(cards, "A_LEVI_2");
        // Migrate the earlier generated form in place to preserve its GUID and references.
        if (upgraded == null) upgraded = FindApostle(cards, "A_LEVI_UPGRADED");
        if (upgraded == null)
        {
            upgraded = Make<ApostleData>("A_LEVI_2");
            Configure(upgraded, s => {
                s.FindProperty("cardID").stringValue = "A_LEVI_2";
                s.FindProperty("cardName").stringValue = "Levi (Upgraded)";
                s.FindProperty("collectible").boolValue = false;
                s.FindProperty("artwork").objectReferenceValue = levi.artwork;
            });
        }
        Configure(upgraded, s => {
            s.FindProperty("cardID").stringValue = "A_LEVI_2";
            s.FindProperty("collectible").boolValue = false;
        });
        foreach (var form in new[] { levi, upgraded })
            Configure(form, s => {
                s.FindProperty("manaCost").intValue = 3;
                s.FindProperty("attack").intValue = form == levi ? 2 : 4;
                s.FindProperty("health").intValue = form == levi ? 2 : 4;
                s.FindProperty("cardRace").enumValueIndex = (int)CardRace.Witch;
            });
        var transform = Make<TransformAndRewardEffect>("FX_LeviGraduateAndReward");
        Configure(transform, s => {
            s.FindProperty("transformInto").objectReferenceValue = upgraded;
            s.FindProperty("reward").objectReferenceValue = reward;
            s.FindProperty("targetType").enumValueIndex = (int)EffectTargetType.Self;
        });
        var friendly = Make<EventCardTriggerCondition>("COND_LeviFriendlySpell");
        Configure(friendly, s => {
            s.FindProperty("relation").enumValueIndex = (int)EventCardTriggerCondition.Relation.Friendly;
            s.FindProperty("filterType").boolValue = true;
            s.FindProperty("cardType").enumValueIndex = (int)CardType.Spell;
        });
        var quest = Make<TriggeredEffect>("FX_LeviFiveSpellsQuest");
        Configure(quest, s => {
            s.FindProperty("triggerType").enumValueIndex = (int)CardTriggerType.SpellCast;
            s.FindProperty("requiredOccurrences").intValue = 5;
            s.FindProperty("resetEachTurn").boolValue = true;
            s.FindProperty("repeatAfterThreshold").boolValue = false;
            s.FindProperty("actorMustBeSelf").boolValue = false;
            s.FindProperty("matchRole").enumValueIndex = (int)TriggeredEffect.EventRole.Subject;
            var conditions = s.FindProperty("conditions"); conditions.arraySize = 1;
            conditions.GetArrayElementAtIndex(0).objectReferenceValue = friendly;
            var effects = s.FindProperty("effects"); effects.arraySize = 1;
            effects.GetArrayElementAtIndex(0).objectReferenceValue = transform;
        });
        Configure(levi, s => {
            s.FindProperty("battlecry").objectReferenceValue = null;
            s.FindProperty("passive").objectReferenceValue = quest;
            s.FindProperty("description").stringValue = "While on the field, cast 5 spells in one turn to transform. Your spells cost 1 less for the rest of this game.";
        });
        Configure(upgraded, s => s.FindProperty("description").stringValue = "Quest completed: your spells cost 1 less for the rest of this game.");
        var database = AssetDatabase.LoadAssetAtPath<CardDatabase>("Assets/Cards/Database/CardDatabase.asset");
        Configure(database, s => {
            var list = s.FindProperty("allCards");
            foreach (var form in new[] { levi, upgraded })
            {
                bool present = false;
                for (int i = 0; i < list.arraySize; i++) if (list.GetArrayElementAtIndex(i).objectReferenceValue == form) present = true;
                if (!present) { int index = list.arraySize; list.arraySize++; list.GetArrayElementAtIndex(index).objectReferenceValue = form; }
            }
        });
        var rows = CardDataCsv.ReadLocalizationRows();
        PutTranslation(rows, "A_LEVI_NAME", "Levi", "레비");
        PutTranslation(rows, "A_LEVI_TEXT", levi.description, "필드에 있는 동안 한 턴에 마법 5번 사용 시 졸업. 이번 게임 동안 내 마법 비용 −1.");
        PutTranslation(rows, "A_LEVI_2_NAME", "Levi (Graduated)", "레비(졸업)");
        PutTranslation(rows, "A_LEVI_2_TEXT", upgraded.description, "졸업 완료: 이번 게임 동안 내 마법 비용 −1.");
        var csv = new StringBuilder();
        foreach (var row in rows) CardDataCsv.AppendRow(csv, row);
        File.WriteAllText(CardDataCsv.LocalizationPath, csv.ToString(), new UTF8Encoding(true));
        AssetDatabase.ImportAsset(CardDataCsv.LocalizationPath);
    }
    private static ApostleData FindApostle(List<CardData> cards, string id)
    {
        var card = cards.SingleOrDefault(c => c.CardID == id);
        if (card != null && !(card is ApostleData)) throw new InvalidOperationException(id + " must be ApostleData.");
        return card as ApostleData;
    }
    private static void PutTranslation(List<List<string>> rows, string key, string english, string korean)
    {
        var row = rows.Skip(1).SingleOrDefault(r => r[0] == key);
        if (row == null) rows.Add(new List<string> { key, english, korean });
        else { row[1] = english; row[2] = korean; }
    }
    private static void AttachBreadTrigger(CardData card, string name, CardTriggerType type, CardEffect child, params TriggerCondition[] conditions)
    {
        if (!(card is MinionData)) return;
        var effect = Make<TriggeredEffect>(name);
        Configure(effect, s => {
            s.FindProperty("triggerType").enumValueIndex = (int)type;
            s.FindProperty("requiredOccurrences").intValue = 1;
            s.FindProperty("repeatAfterThreshold").boolValue = true;
            s.FindProperty("actorMustBeSelf").boolValue = false;
            var list = s.FindProperty("conditions"); list.arraySize = conditions.Length;
            for (int i = 0; i < conditions.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = conditions[i];
            var effects = s.FindProperty("effects"); effects.arraySize = 1;
            effects.GetArrayElementAtIndex(0).objectReferenceValue = child;
        });
        Configure(card, s => {
            // These old triggers conflict with the latest designs.
            s.FindProperty("battlecry").objectReferenceValue = null;
            s.FindProperty("turnEnd").objectReferenceValue = null;
            var list = s.FindProperty("additionalPassives");
            for (int i = 0; i < list.arraySize; i++)
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == effect) return;
            int index = list.arraySize; list.arraySize++;
            list.GetArrayElementAtIndex(index).objectReferenceValue = effect;
        });
    }
    private static TriggeredEffect Trigger(string name, int count, bool repeat, CardEffect child)
    {
        var effect = Make<TriggeredEffect>(name);
        Configure(effect, s => {
            s.FindProperty("triggerType").enumValueIndex = (int)CardTriggerType.UnitKilled;
            s.FindProperty("actorMustBeSelf").boolValue = true;
            s.FindProperty("resetEachTurn").boolValue = false;
            s.FindProperty("requiredOccurrences").intValue = count;
            s.FindProperty("repeatAfterThreshold").boolValue = repeat;
            s.FindProperty("matchRole").enumValueIndex = (int)TriggeredEffect.EventRole.Subject;
            var effects = s.FindProperty("effects"); effects.arraySize = 1;
            effects.GetArrayElementAtIndex(0).objectReferenceValue = child;
        });
        return effect;
    }
    private static T Make<T>(string name) where T : ScriptableObject
    {
        string path = Folder + "/" + name + ".asset";
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("Asset type conflict: " + path);
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        Undo.RegisterCreatedObjectUndo(asset, "Create trigger asset");
        return asset;
    }
    private static void Configure(UnityEngine.Object asset, Action<SerializedObject> action)
    {
        Undo.RecordObject(asset, "Configure trigger asset");
        var s = new SerializedObject(asset); action(s); s.ApplyModifiedProperties(); EditorUtility.SetDirty(asset);
    }
}
