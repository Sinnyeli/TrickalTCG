using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class MissingSpellEffectsInitializer
{
    public static void Build()
    {
        var cards = CardDataCsv.FindCards().ToList();
        foreach (string id in new[] { "S_ENDURE", "S_SLEEVE_IN", "S_MORALE_BOOST", "S_SURPRISE_BOX", "S_MEDITATION_TIME", "S_PASTEL_PICNIC", "S_FEAT_OF_STRENGTH", "S_SODA_CAPSULE", "S_MEMBERSHIP_CARD", "S_TAKE_HITS_PAINLESSLY", "S_FINAL_SPRINT", "S_MASTER_OF_COMBAT", "S_GET_THAT_ONE", "S_CRITICAL_STRIKE", "S_OPEN_RUN", "S_EARLY_LEAVE", "S_MOW", "S_SUSPICIOUS_POTION", "S_STRAWBERRY_CAPSULE" })
        {
            var card = cards.SingleOrDefault(c => c.CardID == id) as SpellData;
            var effect = AssetDatabase.LoadAssetAtPath<DescribedSpellEffect>("Assets/Cards/Effects/MissingSpells/FX_" + id + ".asset");
            if (card == null || effect == null) throw new InvalidOperationException("Missing spell or effect: " + id);
            var serialized = new SerializedObject(card);
            var effects = serialized.FindProperty("spellEffects"); effects.arraySize = 1;
            effects.GetArrayElementAtIndex(0).objectReferenceValue = effect;
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(card);
        }
        var surprise = AssetDatabase.LoadAssetAtPath<DescribedSpellEffect>("Assets/Cards/Effects/MissingSpells/FX_S_SURPRISE_BOX.asset");
        var data = new SerializedObject(surprise);
        var pool = data.FindProperty("randomSpells");
        var spells = cards.OfType<SpellData>().Where(c => c.Collectible && c.CardID != "S_SURPRISE_BOX" && c.SpellEffects.Any(e => e != null)).ToList();
        pool.arraySize = spells.Count;
        for (int i = 0; i < spells.Count; i++) pool.GetArrayElementAtIndex(i).objectReferenceValue = spells[i];
        data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(surprise);
        AssetDatabase.SaveAssets();
    }
}
