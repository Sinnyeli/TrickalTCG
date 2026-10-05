using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class GhostAbilityInitializer
{
    private const string Folder = "Assets/Cards/Effects/GhostBatch";
    public static void Build()
    {
        var cards = CardDataCsv.FindCards();
        var ids = new[] { "A_ALICE", "A_BARONG", "A_ESPI", "A_KISHA", "A_LETHE", "A_MAISON", "A_CRIM", "A_RIM", "A_SARI", "A_SELENE", "A_SHAYDIT", "A_SHAYDI", "A_SPEAKI", "A_VELA", "A_VEROO", "A_XION" };
        foreach (var id in ids) Require(cards, id);
        var pumpkin = cards.SingleOrDefault(c => c.CardID == "M_PUMPKIN") as MinionData;
        if (pumpkin == null) throw new InvalidOperationException("M_PUMPKIN MinionData is required.");
        var database = AssetDatabase.LoadAssetAtPath<CardDatabase>("Assets/Cards/Database/CardDatabase.asset");
        if (database == null) throw new InvalidOperationException("CardDatabase.asset is required.");
        var rows = CardDataCsv.ReadLocalizationRows();
        EnsureFolder(Folder);
        var past = Token<SpellData>(cards, "S_ALICE_PAST", "Past", 3);
        var present = Token<SpellData>(cards, "S_ALICE_PRESENT", "Present", 3);
        var future = Token<SpellData>(cards, "S_ALICE_FUTURE", "Future", 3);
        var mana = Make<IncreaseMaxManaEffect>("FX_AlicePastMana");
        Edit(mana, x => { Target(x, EffectTargetType.None); x.FindProperty("amount").intValue = 1; });
        var buff = Make<BuffEffect>("FX_AlicePresentBuff22");
        Edit(buff, x => { Target(x, EffectTargetType.AllFriendlyUnits); x.FindProperty("attackAmount").intValue = 2; x.FindProperty("healthAmount").intValue = 2; });
        var summon = Make<SummonFromDeckEffect>("FX_AliceFutureSummon");
        Edit(summon, x => { Target(x, EffectTargetType.None); x.FindProperty("amount").intValue = 1; });
        var prophecies = new[] { past, present, future };
        var spellEffects = new CardEffect[] { mana, buff, summon };
        for (int i = 0; i < prophecies.Length; i++)
        {
            int index = i;
            var draw = Make<AliceDestinyDrawEffect>("FX_AliceDestiny" + i);
            Edit(draw, x => { Target(x, EffectTargetType.None); x.FindProperty("kind").intValue = index; });
            Edit(prophecies[i], x => { Refs(x, "spellEffects", new UnityEngine.Object[] { spellEffects[index] }); Refs(x, "onDrawEffects", new UnityEngine.Object[] { draw }); });
        }
        Register(database, prophecies);
        var alice = Bind<AliceProphecySeedEffect>(Require(cards, "A_ALICE"), "FX_AliceSeed", EffectTargetType.None);
        Edit(alice, x => Refs(x, "prophecies", prophecies.Cast<UnityEngine.Object>().ToArray()));
        var mark = Bind<GhostDeathMarkEffect>(Require(cards, "A_BARONG"), "FX_BarongMark", EffectTargetType.AnyUnit);
        Edit(mark, x => { x.FindProperty("destroyMarked").boolValue = false; x.FindProperty("markingEffect").objectReferenceValue = null; });
        var destroyMark = Bind<GhostDeathMarkEffect>(Require(cards, "A_BARONG"), "FX_BarongDestroyMarked", EffectTargetType.None, "deathrattle");
        Edit(destroyMark, x => { x.FindProperty("destroyMarked").boolValue = true; x.FindProperty("markingEffect").objectReferenceValue = mark; });
        Bind<GhostStealTopEffect>(Require(cards, "A_ESPI"), "FX_EspiStealTop", EffectTargetType.None, "onAttack");
        var kisha = Bind<KishaMusicEffect>(Require(cards, "A_KISHA"), "FX_KishaMusic", EffectTargetType.None);
        // Preserve manual assignments. Automatically bind only an unambiguous MP3 in an Audio folder.
        if (kisha.Track == null)
        {
            var tracks = AssetDatabase.FindAssets("t:AudioClip").Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => string.Equals(Path.GetExtension(path), ".mp3", StringComparison.OrdinalIgnoreCase)
                    && path.Split('/').Any(part => string.Equals(part, "Audio", StringComparison.OrdinalIgnoreCase)))
                .Select(path => AssetDatabase.LoadAssetAtPath<AudioClip>(path)).Where(clip => clip != null).ToList();
            if (tracks.Count == 1) Edit(kisha, x => x.FindProperty("track").objectReferenceValue = tracks[0]);
            else Debug.LogWarning("Kisha: found " + tracks.Count + " MP3 tracks in Audio folders. Assign the intended track to FX_KishaMusic.");
        }
        if (kisha.Track == null) Debug.LogWarning("Kisha needs a track assigned to FX_KishaMusic. Playback is once, without looping.");
        Bind<SilenceEffect>(Require(cards, "A_LETHE"), "FX_LetheSilence", EffectTargetType.AnyUnit);
        SpecificDraw(Require(cards, "A_MAISON"), Require(cards, "A_SARI"), "FX_MaisonDrawSari");
        SpecificDraw(Require(cards, "A_SARI"), Require(cards, "A_VEROO"), "FX_SariDrawVeroo");
        SpecificDraw(Require(cards, "A_VEROO"), Require(cards, "A_MAISON"), "FX_VerooDrawMaison");
        BoardStats(cards, "A_SHAYDI", "FX_ShaydiExchange", GhostBoardStatsEffect.Mode.ExchangeAll);
        BoardStats(cards, "A_SHAYDIT", "FX_TwistedShaydiSwap", GhostBoardStatsEffect.Mode.SwapOthers);
        BoardStats(cards, "A_RIM", "FX_RimLowest", GhostBoardStatsEffect.Mode.LowestOthers);
        BoardStats(cards, "A_CRIM", "FX_ChaosRimRange", GhostBoardStatsEffect.Mode.RandomRangeOthers);
        Bind<GhostCopyStatsEffect>(Require(cards, "A_SPEAKI"), "FX_SpeakiCopyStats", EffectTargetType.AnyUnit);
        var transform = Bind<TransformEffect>(Require(cards, "A_VELA"), "FX_VelaPumpkin", EffectTargetType.AnyUnit);
        Edit(transform, x => x.FindProperty("transformInto").objectReferenceValue = pumpkin);
        var xion = Bind<DamageEffect>(Require(cards, "A_XION"), "FX_XionDamage2", EffectTargetType.AnyUnit);
        Edit(xion, x => x.FindProperty("amount").intValue = 2);
        Keywords(Require(cards, "A_SELENE"), CardKeyword.Bypass, CardKeyword.Taunt);
        Edit(Require(cards, "A_SELENE"), x => x.FindProperty("deathrattle").objectReferenceValue = null);
        Text(Require(cards, "A_ALICE"), rows, "Bypass. Battlecry: Shuffle Past, Present and Future into your deck. Drawing each advances Destiny. Draw all three kinds to win.", "우회. 등장: 과거, 현재, 미래를 내 덱에 섞습니다. 뽑을 때마다 운명 카운터를 얻습니다. 세 종류를 모두 뽑으면 승리합니다.");
        Text(Require(cards, "A_SHAYDI"), rows, "Bypass. Battlecry: Randomly exchange the current Attack and Health values of all units on both sides.", "우회. 등장: 양쪽 모든 유닛의 현재 공격력과 체력 수치를 무작위로 교환합니다.");
        Text(past, rows, "Gain an empty mana crystal. When drawn, advance Destiny by 1.", "빈 마나를 1 얻습니다. 뽑으면 운명 카운터를 1 얻습니다.");
        Text(present, rows, "Give all friendly units +2/+2. When drawn, advance Destiny by 1.", "모든 아군 유닛에게 +2/+2를 부여합니다. 뽑으면 운명 카운터를 1 얻습니다.");
        Text(future, rows, "Summon a random minion from your deck. When drawn, advance Destiny by 1.", "내 덱에서 무작위 유닛을 소환합니다. 뽑으면 운명 카운터를 1 얻습니다.");
        Name(rows, past.CardID, "Past", "과거"); Name(rows, present.CardID, "Present", "현재"); Name(rows, future.CardID, "Future", "미래");
        var csv = new StringBuilder(); foreach (var row in rows) CardDataCsv.AppendRow(csv, row);
        File.WriteAllText(CardDataCsv.LocalizationPath, csv.ToString(), new UTF8Encoding(true));
        AssetDatabase.SaveAssets();
        Debug.Log("16 Ghost definitions configured. Kisha plays the assigned track once; Selene has Bypass and Taunt with no death ability. Add GhostMusicController with a music AudioSource to the scene for playback.");
    }
    private static void SpecificDraw(ApostleData card, CardData drawn, string name)
    {
        var effect = Bind<DrawCardEffect>(card, name, EffectTargetType.None);
        Edit(effect, x => { x.FindProperty("amount").intValue = 1; x.FindProperty("drawType").enumValueIndex = (int)DrawType.Specific; x.FindProperty("specificCard").objectReferenceValue = drawn; });
    }
    private static void BoardStats(List<CardData> cards, string id, string name, GhostBoardStatsEffect.Mode mode)
    {
        var effect = Bind<GhostBoardStatsEffect>(Require(cards, id), name, EffectTargetType.None);
        Edit(effect, x => x.FindProperty("mode").enumValueIndex = (int)mode);
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
