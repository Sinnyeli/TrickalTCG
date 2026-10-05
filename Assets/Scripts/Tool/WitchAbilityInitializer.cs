using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class WitchAbilityInitializer
{
    private const string Folder = "Assets/Cards/Effects/WitchBatch";
    public static void Build()
    {
        var cards = CardDataCsv.FindCards();
        var ids = new[] { "A_ASANA", "A_AYA", "A_BELITA", "A_LEVI", "A_MAKASHA", "A_PICORA", "A_POSHER", "A_PRICKLE", "A_ROULETTE", "A_SHERUM", "A_SNORKI", "A_VARIE", "A_VELVET" };
        foreach (var id in ids) Require(cards, id);
        var laugh = cards.SingleOrDefault(c => c.CardID == "S_MAKASHA") as SpellData;
        if (laugh == null) throw new InvalidOperationException("S_MAKASHA SpellData is required.");
        var database = AssetDatabase.LoadAssetAtPath<CardDatabase>("Assets/Cards/Database/CardDatabase.asset");
        if (database == null) throw new InvalidOperationException("CardDatabase.asset is required.");
        var rows = CardDataCsv.ReadLocalizationRows(); EnsureFolder(Folder);
        var heal = Make<HealEffect>("FX_AsanaHeal1");
        Edit(heal, x => { Target(x, EffectTargetType.Self); x.FindProperty("amount").intValue = 1; });
        var attack = Make<BuffEffect>("FX_WitchAttack1");
        Edit(attack, x => { Target(x, EffectTargetType.Self); x.FindProperty("attackAmount").intValue = 1; x.FindProperty("healthAmount").intValue = 0; });
        var asana = Bind<SharedTargetSequenceEffect>(Require(cards, "A_ASANA"), "FX_AsanaHealAndAttack", EffectTargetType.Self, "resonance");
        Edit(asana, x => Refs(x, "effects", new UnityEngine.Object[] { heal, attack }));
        Edit(Require(cards, "A_VELVET"), x => x.FindProperty("resonance").objectReferenceValue = attack);
        var aya = Bind<WitchThirdResonanceEffect>(Require(cards, "A_AYA"), "FX_AyaThirdDraw", EffectTargetType.None, "resonance");
        Edit(aya, x => x.FindProperty("copyLastSpell").boolValue = false);
        var sherum = Bind<WitchThirdResonanceEffect>(Require(cards, "A_SHERUM"), "FX_SherumThirdCopy", EffectTargetType.None, "resonance");
        Edit(sherum, x => x.FindProperty("copyLastSpell").boolValue = true);
        var belita = Bind<WitchSpellCostAuraEffect>(Require(cards, "A_BELITA"), "FX_BelitaSpellDiscount", EffectTargetType.None, "passive");
        Edit(belita, x => x.FindProperty("amount").intValue = 1);
        ConfigureLevi(cards, database, rows);
        var createLaugh = Bind<CreateCardsEffect>(Require(cards, "A_MAKASHA"), "FX_MakashaThreeLaughs", EffectTargetType.None);
        Edit(createLaugh, x => { x.FindProperty("cardToCreate").objectReferenceValue = laugh; x.FindProperty("amount").intValue = 3; });
        var sound = laugh.SpellEffects.OfType<PlaySoundEffect>().FirstOrDefault() ?? Make<PlaySoundEffect>("FX_MakashaLaughSound"); Edit(sound, x => Target(x, EffectTargetType.None));
        if (sound.Clip == null)
        {
            var clips = AssetDatabase.FindAssets("t:AudioClip").Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => { string name = Path.GetFileNameWithoutExtension(path); return name.IndexOf("makasha", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("laugh", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("ehehe", StringComparison.OrdinalIgnoreCase) >= 0 || name.Contains("이히히"); })
                .Select(path => AssetDatabase.LoadAssetAtPath<AudioClip>(path)).Where(clip => clip != null).ToList();
            if (clips.Count == 1) Edit(sound, x => x.FindProperty("clip").objectReferenceValue = clips[0]);
        }
        if (sound.Clip == null) Debug.LogWarning("Makasha's Laugh is assigned, but the intended laugh clip is missing. Assign it to FX_MakashaLaughSound.");
        Edit(laugh, x => Refs(x, "spellEffects", new UnityEngine.Object[] { sound }));
        var stickers = new List<CardData>();
        var keywords = new[] { CardKeyword.Taunt, CardKeyword.Bypass, CardKeyword.Rush, CardKeyword.Endure, CardKeyword.FirstStrike };
        var stickerIds = new[] { "S_STICKER_SHIELD", "S_STICKER_SKULL", "S_STICKER_SHUPANG", "S_STICKER_ENDURE", "S_STICKER_FIRST_STRIKE" };
        var names = new[] { "Shield Sticker", "Skull Sticker", "Shupang Sticker", "Endure Sticker", "First Strike Sticker" };
        var korean = new[] { "방패 스티커", "해골 스티커", "슈팡 스티커", "참기 스티커", "선빵 스티커" };
        for (int i = 0; i < keywords.Length; i++)
        {
            int index = i; var sticker = Token<SpellData>(cards, stickerIds[i], names[i], 1);
            var grant = Make<GrantKeywordEffect>("FX_Sticker" + keywords[i]);
            Edit(grant, x => { Target(x, EffectTargetType.AnyUnit); x.FindProperty("keyword").enumValueIndex = (int)keywords[index]; x.FindProperty("turnEnds").intValue = 0; });
            Edit(sticker, x => Refs(x, "spellEffects", new UnityEngine.Object[] { grant }));
            Name(rows, sticker.CardID, names[i], korean[i]); Text(sticker, rows, "Permanently grant " + keywords[i] + " to a unit.", "유닛에게 " + keywords[i] + " 키워드를 영구적으로 부여합니다.");
            stickers.Add(sticker);
        }
        Register(database, stickers.ToArray());
        var picora = Bind<CreateRandomCardEffect>(Require(cards, "A_PICORA"), "FX_PicoraRandomSticker", EffectTargetType.None);
        Edit(picora, x => { Refs(x, "cardPool", stickers.Cast<UnityEngine.Object>().ToArray()); x.FindProperty("amount").intValue = 1; });
        Edit(Require(cards, "A_PICORA"), x => x.FindProperty("turnEnd").objectReferenceValue = picora);
        ConfigurePosher(cards, database, rows);
        var prickle = Bind<DamageEffect>(Require(cards, "A_PRICKLE"), "FX_PrickleEnemyHero1", EffectTargetType.EnemyHero, "resonance");
        Edit(prickle, x => x.FindProperty("amount").intValue = 1);
        Bind<RouletteBothPlayersEffect>(Require(cards, "A_ROULETTE"), "FX_RouletteSharedRoll", EffectTargetType.None);
        var varie = Bind<WitchRecoverSpellsEffect>(Require(cards, "A_VARIE"), "FX_VarieRecoverTwoSpells", EffectTargetType.None);
        Edit(varie, x => x.FindProperty("amount").intValue = 2);
        // Snorki is the saved 7/7 vanilla unit; no invented ability.
        Text(Require(cards, "A_MAKASHA"), rows, "Battlecry: Create 3 Makasha's Laugh spells in your hand.", "등장: 패에 '이히히~' 3장을 생성합니다.");
        Text(Require(cards, "A_PRICKLE"), rows, "Resonance: Deal 1 damage to the enemy hero.", "공명: 적 영웅에게 피해를 1 줍니다.");
        Text(Require(cards, "A_ROULETTE"), rows, "Battlecry: One shared 25% roll: both players draw 1, destroy all minions, each summons a random minion from their deck if possible, or each discards a random card.", "등장: 각 25% 확률로 양쪽 모두 1장 뽑기, 모든 유닛 파괴, 각 덱에서 무작위 유닛 소환, 또는 각자 무작위 카드 1장 버리기 중 같은 결과를 적용합니다.");
        var csv = new StringBuilder(); foreach (var row in rows) CardDataCsv.AppendRow(csv, row);
        File.WriteAllText(CardDataCsv.LocalizationPath, csv.ToString(), new UTF8Encoding(true)); AssetDatabase.SaveAssets();
        Debug.Log("Witch batch configured: 13 definitions reviewed. Existing newer Levi effects are preserved. Posher uses the three confirmed potions; assign Makasha audio if missing.");
    }
    private static void ConfigurePosher(List<CardData> cards, CardDatabase database, List<List<string>> rows)
    {
        var berserk = Token<SpellData>(cards, "S_POSHER_BERSERK", "Berserk Potion", 1);
        var defense = Token<SpellData>(cards, "S_POSHER_DEFENSE", "Defense Potion", 1);
        var caffeine = Token<SpellData>(cards, "S_POSHER_CAFFEINE", "High-Caffeine Drink", 1);
        var attack = Make<BuffEffect>("FX_PosherAttack2");
        Edit(attack, x => { Target(x, EffectTargetType.AnyUnit); x.FindProperty("attackAmount").intValue = 2; x.FindProperty("healthAmount").intValue = 0; });
        var damage = Make<DamageEffect>("FX_PosherSelfDamage1");
        Edit(damage, x => { Target(x, EffectTargetType.AnyUnit); x.FindProperty("amount").intValue = 1; });
        var berserkSequence = Make<SharedTargetSequenceEffect>("FX_PosherBerserkSequence");
        Edit(berserkSequence, x => { Target(x, EffectTargetType.AnyUnit); Refs(x, "effects", new UnityEngine.Object[] { attack, damage }); });
        var defenseBuff = Make<BuffEffect>("FX_PosherDefenseBuff");
        Edit(defenseBuff, x => { Target(x, EffectTargetType.AnyUnit); x.FindProperty("attackAmount").intValue = -1; x.FindProperty("healthAmount").intValue = 2; });
        var caffeineBuff = Make<BuffEffect>("FX_PosherCaffeineBuff");
        Edit(caffeineBuff, x => { Target(x, EffectTargetType.AnyUnit); x.FindProperty("attackAmount").intValue = 2; x.FindProperty("healthAmount").intValue = 2; });
        var death = Make<DestroyAtTurnEndEffect>("FX_PosherCaffeineDeath"); Edit(death, x => Target(x, EffectTargetType.AnyUnit));
        var caffeineSequence = Make<SharedTargetSequenceEffect>("FX_PosherCaffeineSequence");
        Edit(caffeineSequence, x => { Target(x, EffectTargetType.AnyUnit); Refs(x, "effects", new UnityEngine.Object[] { caffeineBuff, death }); });
        Edit(berserk, x => Refs(x, "spellEffects", new UnityEngine.Object[] { berserkSequence }));
        Edit(defense, x => Refs(x, "spellEffects", new UnityEngine.Object[] { defenseBuff }));
        Edit(caffeine, x => Refs(x, "spellEffects", new UnityEngine.Object[] { caffeineSequence }));
        var pool = new CardData[] { berserk, defense, caffeine }; Register(database, pool);
        var generator = Bind<CreateRandomCardEffect>(Require(cards, "A_POSHER"), "FX_PosherRandomPotion", EffectTargetType.None);
        Edit(generator, x => { Refs(x, "cardPool", pool.Cast<UnityEngine.Object>().ToArray()); x.FindProperty("amount").intValue = 1; });
        Name(rows, berserk.CardID, "Berserk Potion", "광전사 포션");
        Name(rows, defense.CardID, "Defense Potion", "방어 포션");
        Name(rows, caffeine.CardID, "High-Caffeine Drink", "고카페인 드링크");
        Text(berserk, rows, "Give a unit +2 Attack, then deal 1 damage to it.", "유닛에게 공격력 +2를 부여한 뒤 피해를 1 줍니다.");
        Text(defense, rows, "Give a unit -1 Attack and +2 Health.", "유닛에게 공격력 -1과 체력 +2를 부여합니다.");
        Text(caffeine, rows, "Give a unit +2/+2. It dies at the end of the current turn.", "유닛에게 +2/+2를 부여합니다. 그 유닛은 이번 턴 종료 시 죽습니다.");
        Text(Require(cards, "A_POSHER"), rows, "Battlecry: Create a random Berserk Potion, Defense Potion or High-Caffeine Drink.", "등장: 광전사 포션, 방어 포션, 고카페인 드링크 중 무작위 1장을 생성합니다.");
    }
    private static void ConfigureLevi(List<CardData> cards, CardDatabase database, List<List<string>> rows)
    {
        var levi = Require(cards, "A_LEVI");
        bool configured = levi.Passives.Any() || levi.Battlecry != null || levi.TurnStart != null || levi.TurnEnd != null || levi.Resonance != null || levi.OnAttack != null || levi.OnDamageTaken != null || levi.Deathrattle != null;
        if (configured && !(levi.Resonance is LeviTurnStudyEffect))
        { Debug.Log("Levi already has effects: preserving the newer quest implementation and stats."); return; }
        var graduate = Token<ApostleData>(cards, "A_LEVI_2", "Graduate Levi", 3);
        Edit(graduate, x => { x.FindProperty("attack").intValue = 4; x.FindProperty("health").intValue = 4; x.FindProperty("cardRace").enumValueIndex = (int)CardRace.Witch; });
        Edit(levi, x => { x.FindProperty("manaCost").intValue = 3; x.FindProperty("attack").intValue = 2; x.FindProperty("health").intValue = 2; });
        var quest = Bind<LeviTurnStudyEffect>(levi, "FX_LeviFiveSpellsInTurn", EffectTargetType.None, "resonance");
        Edit(quest, x => x.FindProperty("graduate").objectReferenceValue = graduate);
        Register(database, graduate);
        Text(levi, rows, "While on the field, cast 5 spells in one turn to Graduate. Progress resets each turn. Graduation permanently reduces your spell costs by 1, once per player.", "필드에 있는 동안 한 턴에 마법 5개를 사용하면 졸업합니다. 진행도는 턴마다 초기화됩니다. 졸업하면 플레이어당 한 번, 내 마법 비용이 영구적으로 1 감소합니다.");
        Name(rows, graduate.CardID, "Graduate Levi", "졸업한 레비"); Text(graduate, rows, "Graduated. Your graduation reward is permanent.", "졸업했습니다. 졸업 보상은 영구적으로 유지됩니다.");
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
